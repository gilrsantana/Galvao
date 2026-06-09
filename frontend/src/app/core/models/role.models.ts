export interface RoleResponse {
  id: string;
  name: string;
  description: string;
}

export interface CreateRoleRequest {
  roleName: string;
  description: string;
}

export interface AssignRoleRequest {
  userId: string;
  roleName: string;
}
