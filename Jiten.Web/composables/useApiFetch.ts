import { PaginatedResponse } from '~/types/types';
import type { AsyncDataRequestStatus, UseFetchOptions } from '#app';

export function useApiFetch<T>(
  request: string | (() => string),
  opts?: any
): Promise<{
  data: Ref<T | null>;
  status: Ref<AsyncDataRequestStatus>;
  error: Ref<Error | null>;
}> {
  const tokenCookie = useCookie('token');

  // Create a unique key for this request to prevent duplicates
  const key = generateRequestKey(request);
  const uniqueKey = `api-${key}-${safeStringifyQuery(opts?.query)}`;

  // Set default headers
  const headers = new Headers(opts?.headers || {});

  // Add authorization header if token exists
  if (tokenCookie.value) {
    headers.set('Authorization', `Bearer ${tokenCookie.value}`);
  }

  // Merge options with headers
  const options = {
    ...opts,
    headers,
    key: uniqueKey,
    server: opts?.server ?? true,
    lazy: opts?.lazy ?? false,
  };

  const { data, status, error } = useFetch<T>(request, { 
    baseURL: useRuntimeConfig().public.baseURL,
    ...options
  });

  return { data, status, error };
}

export  function useApiFetchPaginated<T>(
  request: string | (() => string),
  opts?: any
): Promise<{
  data: Ref<PaginatedResponse<T> | null>;
  status: Ref<AsyncDataRequestStatus>;
  error: Ref<Error | null>;
}> {
  const config = useRuntimeConfig();
  const tokenCookie = useCookie('token');

  // Create a unique key for this request to prevent duplicates
  const key = generateRequestKey(request);
  const uniqueKey = `api-${key}-${safeStringifyQuery(opts?.query)}`;

  // Set default headers
  const headers = new Headers(opts?.headers || {});

  // Add authorization header if token exists
  if (tokenCookie.value) {
    headers.set('Authorization', `Bearer ${tokenCookie.value}`);
  }

  // Merge options with headers
  const options = {
    ...opts,
    headers,
    key: uniqueKey, // This prevents duplicate requests
    server: opts?.server ?? true,
    lazy: opts?.lazy ?? false,
  };

  // Use useFetch without await, keeping the data reactive
  const { data, status, error } = useFetch<PaginatedResponse<T>>(request, {
    baseURL: config.public.baseURL,
    ...options,
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
    error,
  };
}

// Helper function to generate a safe key from request parameter
const generateRequestKey = (request: string | (() => string)) => {
  if (typeof request === 'string') {
    return request;
  } else if (typeof request === 'function') {
    try {
      return request();
    } catch (e) {
      return 'dynamic-request';
    }
  } else {
    return String(request);
  }
};

// Helper function to safely stringify query parameters
const safeStringifyQuery = (query: any) => {
  if (!query || typeof query !== 'object') return '{}';

  try {
    // Convert reactive values to their actual values
    const plainQuery: Record<string, any> = {};
    for (const [key, value] of Object.entries(query)) {
      // Handle Vue refs and computed values
      if (value && typeof value === 'object' && 'value' in value) {
        plainQuery[key] = value.value;
      } else {
        plainQuery[key] = value;
      }
    }
    return JSON.stringify(plainQuery);
  } catch (e) {
    // Fallback if JSON.stringify still fails
    return Object.keys(query).sort().join('-');
  }
};
