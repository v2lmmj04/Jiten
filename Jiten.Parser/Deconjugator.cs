/// Reimplementation from https://github.com/wareya/nazeka/blob/master/background-script.js
/// Original code is licenced under Apache 2.0 https://www.apache.org/licenses/LICENSE-2.0

using System.Text.Json;

namespace Jiten.Parser;

public class Deconjugator
{
    public List<DeconjugationRule> Rules = new();

    public Deconjugator()
    {
        var options = new JsonSerializerOptions
                      {
                          AllowTrailingCommas = true,
                          ReadCommentHandling = JsonCommentHandling.Skip,
                          Converters = { new StringArrayConverter() }
                      };

        var rules = JsonSerializer.Deserialize<List<DeconjugationRule>>(File.ReadAllText("resources/deconjugator.json"), options);
        foreach (var rule in rules)
        {
            Rules.Add(rule);
        }
    }

    public HashSet<DeconjugationForm> Deconjugate(string text)
    {
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
            foreach (var rule in Rules)
            {
                var newForm = rule.Type switch
                {
                    "stdrule" => StdRuleDeconjugate(startForm, rule),
                    "rewriterule" => RewriteRuleDeconjugate(startForm, rule),
                    "onlyfinalrule" => OnlyFinalRuleDeconjugate(startForm, rule),
                    "neverfinalrule" => NeverFinalRuleDeconjugate(startForm, rule),
                    "contextrule" => ContextRuleDeconjugate(startForm, rule),
                    "substitution" => SubstitutionDeconjugate(startForm, rule),
                    _ => null
                };

                if (newForm == null)
                    continue;

                foreach (var form in newForm)
                {
                    if (form != null && !processed.Contains(form) && !novel.Contains(form) && !newNovel.Contains(form))
                        newNovel.Add(form);
                }

                processed.UnionWith(novel);
                novel = newNovel;
            }
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

        string[]? array = null;

        // pick the first one that is an array
        if (rule.DecEnd is { Length: > 0 })
            array = rule.DecEnd;
        else if (rule.ConEnd is { Length: > 0 })
            array = rule.ConEnd;
        else if (rule.DecTag is { Length: > 0 })
            array = rule.DecTag;
        else if (rule.ConTag is { Length: > 0 })
            array = rule.ConTag;

        if (array == null)
        {
            var virtualRule = new DeconjugationVirtualRule(rule.DecEnd[0], rule.ConEnd[0], rule.DecTag![0], rule.ConTag![0], rule.Detail);

            return StdRuleDeconjugateInner(form, virtualRule) is { } hit ? [hit] : null;
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

        string[]? array = null;

        // pick the first one that is an array
        if (rule.DecEnd is { Length: > 0 })
            array = rule.DecEnd;
        else if (rule.ConEnd is { Length: > 0 })
            array = rule.ConEnd;

        if (array == null)
        {
            var virtualRule = new DeconjugationVirtualRule(rule.DecEnd[0], rule.ConEnd[0], rule.DecTag![0], rule.ConTag![0], rule.Detail);

            return SubstitutionInner(form, virtualRule) is { } hit ? [hit] : null;
        }
        else
        {
            var collection = new HashSet<DeconjugationForm>();
            for (int i = 0; i < array.Length; i++)
            {
                var virtualRule = new DeconjugationVirtualRule(
                                                               rule.DecEnd.ElementAtOrDefault(i) ?? rule.DecEnd[0],
                                                               rule.ConEnd.ElementAtOrDefault(i) ?? rule.ConEnd[0],
                                                               null,
                                                               null,
                                                               rule.Detail
                                                              );

                if (SubstitutionInner(form, virtualRule) is { } ret)
                    collection.Add(ret);
            }

            return collection;
        }
    }

    private DeconjugationForm SubstitutionInner(DeconjugationForm form, DeconjugationVirtualRule rule)
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