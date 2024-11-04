import {PaginatedResponse} from "~/types";

export async function useApiFetch<T>(request: string, opts?: any): Promise<{ data: T; pending: boolean; error: any }> {
    const config = useRuntimeConfig();

    const {data, status, error} = await useFetch<T>(request, {baseURL: config.public.baseURL, ...opts});

    return {data, status, error};
}

export function useApiFetchPaginated<T>(request: string, opts?: any) {
    const config = useRuntimeConfig();

    // Use useFetch without await, keeping the data reactive
    const {data, status, error} = useFetch<PaginatedResponse<T>>(request, {
        baseURL: config.public.baseURL,
        ...opts
    });

    const paginatedData = computed(() => {
        if (data.value) {
            return new PaginatedResponse<T>(
                data.value.data,
                data.value.totalItems,
                data.value.pageSize,
                data.value.currentOffset
            );
        }
        return null;
    });

    return {
        data: paginatedData,
        status,
        error
    };
}
