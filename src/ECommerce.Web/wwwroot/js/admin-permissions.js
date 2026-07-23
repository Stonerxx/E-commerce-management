(function () {
    const { createApp } = Vue;

    createApp({
        data() {
            return {
                saving: false,
                error: "",
                message: "",
                keyword: "",
                roles: [],
                permissions: [],
                selectedRole: null,
                selectedRoleId: null,
                selectedPermissionIds: []
            };
        },
        computed: {
            isAdminRole() {
                return this.selectedRole?.roleName?.toUpperCase() === "ADMIN";
            }
        },
        async mounted() {
            await this.loadRoles();
            await this.loadPermissions();
        },
        methods: {
            async loadRoles() {
                const payload = await this.request("/api/v1/admin/permissions/roles");
                this.roles = payload.data || [];
                if (this.roles.length > 0) {
                    await this.selectRole(this.roles[0]);
                }
            },
            async loadPermissions() {
                const params = new URLSearchParams();
                if (this.keyword) params.set("keyword", this.keyword);
                const payload = await this.request(`/api/v1/admin/permissions?${params.toString()}`);
                this.permissions = payload.data || [];
            },
            async selectRole(role) {
                this.selectedRole = role;
                this.selectedRoleId = role.roleId;
                this.error = "";
                this.message = "";
                const payload = await this.request(`/api/v1/admin/permissions/roles/${role.roleId}`);
                this.selectedPermissionIds = (payload.data || []).map(item => item.permissionId);
            },
            async save() {
                if (this.isAdminRole) {
                    this.error = "ADMIN 是内置超级管理员，权限不可修改";
                    return;
                }

                this.error = "";
                this.message = "";
                this.saving = true;
                try {
                    await this.request(`/api/v1/admin/permissions/roles/${this.selectedRoleId}`, "PUT", {
                        permissionIds: this.selectedPermissionIds
                    });
                    this.message = "角色权限绑定成功";
                } catch (error) {
                    this.error = error instanceof Error ? error.message : "角色权限保存失败";
                } finally {
                    this.saving = false;
                }
            },
            async request(url, method = "GET", body = null) {
                const response = await fetch(url, {
                    method,
                    headers: {
                        "Accept": "application/json",
                        "Content-Type": "application/json"
                    },
                    body: body ? JSON.stringify(body) : null
                });
                const payload = await response.json();
                if (!response.ok || !payload.success) {
                    throw new Error(payload.message || `HTTP ${response.status}`);
                }

                return payload;
            }
        }
    }).mount("#adminPermissionsApp");
})();
