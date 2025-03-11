/// Reimplementation from https://github.com/wareya/nazeka/blob/master/background-script.js
/// Original code is licenced under Apache 2.0 https://www.apache.org/licenses/LICENSE-2.0

using System.Collections.Concurrent;
using System.Text.Json;

namespace Jiten.Parser;

public class Deconjugator
{
    public List<DeconjugationRule> Rules = new();

    private static readonly ConcurrentDictionary<string, HashSet<DeconjugationForm>> _deconjugationCache
        = new(StringComparer.Ordinal);

    public Deconjugator()
    {
        var options = new JsonSerializerOptions
                      {
                          AllowTrailingCommas = true,
                          ReadCommentHandling = JsonCommentHandling.Skip,
                          Converters = { new StringArrayConverter() }
                      };
        var rules =
            JsonSerializer
                .Deserialize<
                    List<DeconjugationRule>>(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources", "deconjugator.json")),
                                             options);
        foreach (var rule in rules)
        {
            Rules.Add(rule);
        }
    }

    public HashSet<DeconjugationForm> Deconjugate(string text)
    {
        if (_deconjugationCache.TryGetValue(text, out var cached))
        {
            return new HashSet<DeconjugationForm>(cached); // Return copy to prevent modification
        }
        
        var processed = new HashSet<DeconjugationForm>();

        if (string.IsNullOrEmpty(text))
            return processed;

        var novel = new HashSet<DeconjugationForm>();

        var startForm = new DeconjugationForm(text: text, originalText: text, tags: new List<string>(), seenText: new HashSet<string>(),
                                              process: new List<string>());

        novel.Add(startForm);

        while (novel.Count > 0)
        {
            var newNovel = new HashSet<DeconjugationForm>();
            foreach (DeconjugationForm form in novel)
            {
                foreach (var rule in Rules)
                {
                    var newForm = rule.Type switch
                    {
                        "stdrule" => StdRuleDeconjugate(form, rule),
                        "rewriterule" => RewriteRuleDeconjugate(form, rule),
                        "onlyfinalrule" => OnlyFinalRuleDeconjugate(form, rule),
                        "neverfinalrule" => NeverFinalRuleDeconjugate(form, rule),
                        "contextrule" => ContextRuleDeconjugate(form, rule),
                        "substitution" => SubstitutionDeconjugate(form, rule),
                        _ => null
                    };

                    if (newForm == null)
                        continue;

                    foreach (var f in newForm)
                    {
                        if (f != null && !processed.Contains(f) && !novel.Contains(f) && !newNovel.Contains(f))
                            newNovel.Add(f);
                    }
                }
            }

            processed.UnionWith(novel);
            novel = newNovel;
        }

        // processed.Remove(startForm);

        if (text.Length <= 20 && _deconjugationCache.Count < 1000000)
        {
            _deconjugationCache[text] = new HashSet<DeconjugationForm>(processed);
        }
        
        return processed;
    }

    private HashSet<DeconjugationForm>? StdRuleDeconjugate(DeconjugationForm form, DeconjugationRule rule)
    {
        // can't deconjugate nothingness
        if (string.IsNullOrEmpty(form.Text))
            return null;

        // deconjugated form too much longer than conjugated form
        if (form.Text.Length > form.OriginalText.Length + 10)
            return null;

        // impossibly information-dense
        if (form.Tags.Count > form.OriginalText.Length + 6)
            return null;

        // blank detail mean it can't be the last (first applied, but rightmost) rule
        if (string.IsNullOrEmpty(rule.Detail) && form.Tags.Count == 0)
            return null;

        string[]? array = rule.DecEnd;
        if (array.Length == 1)
        {
            DeconjugationVirtualRule virtualRule = new(rule.DecEnd[0],
                                                       rule.ConEnd[0],
                                                       rule.DecTag![0],
                                                       rule.ConTag![0],
                                                       rule.Detail
                                                      );

            if (StdRuleDeconjugateInner(form, virtualRule) is { } hit)
                return [hit];

            return null;
        }

        var collection = new HashSet<DeconjugationForm>();
        for (int i = 0; i < array.Length; i++)
        {
            var virtualRule = new DeconjugationVirtualRule(rule.DecEnd.ElementAtOrDefault(i) ?? rule.DecEnd[0],
                                                           rule.ConEnd.ElementAtOrDefault(i) ?? rule.ConEnd[0],
                                                           rule.DecTag!.ElementAtOrDefault(i) ?? rule.DecTag![0],
                                                           rule.ConTag!.ElementAtOrDefault(i) ?? rule.ConTag![0],
                                                           rule.Detail);

            if (StdRuleDeconjugateInner(form, virtualRule) is { } hit)
            {
                collection.Add(hit);
            }
        }

        return collection;
    }

    private DeconjugationForm? StdRuleDeconjugateInner(DeconjugationForm form, DeconjugationVirtualRule rule)
    {
        if (!form.Text.EndsWith(rule.ConEnd, StringComparison.Ordinal))
            return null;

        if (form.Tags.Count > 0 && form.Tags[^1] != rule.ConTag)
            return null;


        var newText = form.Text[..^rule.ConEnd.Length] + rule.DecEnd;

        if (newText == form.OriginalText)
            return null;

        DeconjugationForm newForm = new(text: newText, originalText: form.OriginalText, tags: form.Tags.ToList(),
                                        seenText: [..form.SeenText], process: form.Process.ToList());
        newForm.Process.Add(rule.Detail);

        if (newForm.Tags.Count == 0)
            newForm.Tags.Add(rule.ConTag!);

        newForm.Tags.Add(rule.DecTag!);

        if (newForm.SeenText.Count == 0)
            newForm.SeenText.Add(form.Text);

        newForm.SeenText.Add(newText);

        return newForm;
    }

    private HashSet<DeconjugationForm>? SubstitutionDeconjugate(DeconjugationForm form, DeconjugationRule rule)
    {
        if (form.Process.Count != 0)
            return null;

        // can't deconjugate nothingness
        if (string.IsNullOrEmpty(form.Text))
            return null;


        string[] array = rule.DecEnd;
        if (array.Length is 1)
        {
            if (SubstitutionInner(form, rule) is { } hit)
                return [hit];

            return null;
        }

        var collection = new HashSet<DeconjugationForm>();
        for (int i = 0; i < array.Length; i++)
        {
            var newRule = new DeconjugationRule(
                                                rule.Type,
                                                null,
                                                [rule.DecEnd.ElementAtOrDefault(i) ?? rule.DecEnd[0]],
                                                [rule.ConEnd.ElementAtOrDefault(i) ?? rule.ConEnd[0]],
                                                null,
                                                null,
                                                rule.Detail
                                               );

            if (SubstitutionInner(form, newRule) is { } ret)
                collection.Add(ret);
        }

        return collection;
    }

    private DeconjugationForm SubstitutionInner(DeconjugationForm form, DeconjugationRule rule)
    {
        if (form.Text.Contains(rule.ConEnd[0]))
            return null;

        var newText = form.Text.Replace(rule.ConEnd[0], rule.DecEnd[0]);

        DeconjugationForm newForm = new(text: newText, originalText: form.OriginalText, tags: form.Tags.ToList(),
                                        seenText: [..form.SeenText], process: form.Process.ToList());

        newForm.Process.Add(rule.Detail);

        if (newForm.SeenText.Count == 0)
            newForm.SeenText.Add(form.Text);
        newForm.SeenText.Add(newText);

        return newForm;
    }

    private HashSet<DeconjugationForm>? RewriteRuleDeconjugate(DeconjugationForm form, DeconjugationRule rule)
    {
        if (form.Text != rule.ConEnd[0])
            return null;

        return StdRuleDeconjugate(form, rule);
    }

    private HashSet<DeconjugationForm>? OnlyFinalRuleDeconjugate(DeconjugationForm form, DeconjugationRule rule)
    {
        if (form.Tags.Count != 0)
            return null;

        return StdRuleDeconjugate(form, rule);
    }

    private HashSet<DeconjugationForm>? NeverFinalRuleDeconjugate(DeconjugationForm form, DeconjugationRule rule)
    {
        if (form.Tags.Count == 0)
            return null;

        return StdRuleDeconjugate(form, rule);
    }

    private HashSet<DeconjugationForm>? ContextRuleDeconjugate(DeconjugationForm form, DeconjugationRule rule)
    {
        if (rule.ContextRule == "v1inftrap" && !V1InfTrapCheck(form, rule))
            return null;

        if (rule.ContextRule == "saspecial" && !SaSpecialCheck(form, rule))
            return null;

        return StdRuleDeconjugate(form, rule);
    }

    private bool V1InfTrapCheck(DeconjugationForm form, DeconjugationRule rule)
    {
        if (form.Tags.Count != 1)
            return true;

        if (form.Tags[0] == "stem-ren")
            return false;

        return true;
    }

    private bool SaSpecialCheck(DeconjugationForm form, DeconjugationRule rule)
    {
        if (form.Text == "")
            return false;

        if (!form.Text.EndsWith(rule.ConEnd[0]))
            return false;

        if (form.Text.Substring(0, form.Text.Length - rule.ConEnd[0].Length).EndsWith("さ"))
            return false;

        return true;
    }
}