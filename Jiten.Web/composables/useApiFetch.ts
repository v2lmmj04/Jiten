export async function useApiFetch<T>(request: string, opts?: any): Promise<{ data: T; pending: boolean; error: any }> {
    const config = useRuntimeConfig();

    const { data, pending, error } = await useFetch<T>(request, { baseURL: config.public.baseURL, ...opts });

    return { data, pending, error };
}
