import { useApiQuery, useApiMutation } from '@nucleus/uiplatform-foundation';
import { useQueryClient } from '@tanstack/react-query';
import type { Employee, EmployeeFormValues } from './types';

export interface EmployeeListParams {
  employeeCode?: string;
  sort?: string;
  page?: number;
  pageSize?: number;
}

/** GET /api/employees — the real generic CrudEngine List endpoint, not a mock. */
export function useEmployeeList(params: EmployeeListParams = {}) {
  return useApiQuery<Employee[]>({
    queryKey: ['employees', 'list', params],
    requestConfig: { url: '/employees', method: 'GET', params },
  });
}

export function useEmployee(id: string | undefined) {
  return useApiQuery<Employee>({
    queryKey: ['employees', id],
    requestConfig: { url: `/employees/${id}`, method: 'GET' },
    enabled: Boolean(id),
  });
}

export function useCreateEmployee() {
  const queryClient = useQueryClient();
  return useApiMutation<Employee, EmployeeFormValues>({
    buildRequest: (values) => ({ url: '/employees', method: 'POST', data: values }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['employees'] }),
  });
}

export function useUpdateEmployee(id: string) {
  const queryClient = useQueryClient();
  return useApiMutation<Employee, EmployeeFormValues>({
    buildRequest: (values) => ({ url: `/employees/${id}`, method: 'PUT', data: values }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['employees'] }),
  });
}

export function useDeleteEmployee() {
  const queryClient = useQueryClient();
  return useApiMutation<null, string>({
    buildRequest: (id) => ({ url: `/employees/${id}`, method: 'DELETE' }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['employees'] }),
  });
}
