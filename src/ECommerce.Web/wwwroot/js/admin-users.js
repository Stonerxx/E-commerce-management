(function () {
    const { createApp } = Vue;

    createApp({
        data() {
            return {
                loading: false,
                error: "",
                message: "",
                currentUserId: Number(document.getElementById("adminUsersApp")?.dataset.currentUserId || 0),
                roles: [],
                items: [],
                pageIndex: 1,
                pageSize: 10,
                totalCount: 0,
                totalPages: 0,
                filters: {
                    keyword: "",
                    status: "",
                    role: ""
                }
            };
        },
        async mounted() {
            await this.loadRoles();
            await this.loadUsers();
        },
        methods: {
            isCurrentUser(item) {
                return Number(item.userId) === this.currentUserId;
            },

            async loadRoles() {
                const payload = await this.request("/api/v1/admin/permissions/roles");
                this.roles = payload.data || [];
            },
            async loadUsers() {
                this.loading = true;
                this.error = "";
                this.message = "";

                const params = new URLSearchParams();
                params.set("pageIndex", this.pageIndex.toString());
                params.set("pageSize", this.pageSize.toString());
                if (this.filters.keyword) params.set("keyword", this.filters.keyword);
                if (this.filters.status !== "") params.set("status", this.filters.status);
                if (this.filters.role) params.set("role", this.filters.role);

                try {
                    const payload = await this.request(`/api/v1/admin/users?${params.toString()}`);
                    this.items = payload.data.items || [];
                    this.pageIndex = payload.data.pageIndex;
                    this.pageSize = payload.data.pageSize;
                    this.totalCount = payload.data.totalCount;
                    this.totalPages = payload.data.totalPages;
                } catch (error) {
                    this.error = error instanceof Error ? error.message : "用户查询失败";
                } finally {
                    this.loading = false;
                }
            },
            async toggleStatus(item) {
                const nextStatus = item.status === 1 ? 0 : 1;
                const text = nextStatus === 1 ? "启用" : "禁用";
                if (!confirm(`确认${text}用户 ${item.username} 吗？`)) {
                    return;
                }

                this.error = "";
                this.message = "";
                try {
                    await this.request(`/api/v1/admin/users/${item.userId}/status`, "PUT", { status: nextStatus });
                    this.message = `用户已${text}`;
                    await this.loadUsers();
                } catch (error) {
                    this.error = error instanceof Error ? error.message : "状态修改失败";
                }
            },
            async assignRoles(item) {
                const roleHint = this.roles.map(role => `${role.roleId}=${role.roleName}`).join("，");
                const currentRoleIds = this.roles
                    .filter(role => item.roles.includes(role.roleName))
                    .map(role => role.roleId)
                    .join(",");
                const input = prompt(`输入角色ID，多个角色用英文逗号分隔：${roleHint}`, currentRoleIds);
                if (input === null) {
                    return;
                }

                const roleIds = input.split(",")
                    .map(value => Number(value.trim()))
                    .filter(value => Number.isInteger(value) && value > 0);
                if (roleIds.length === 0) {
                    this.error = "至少选择一个角色";
                    return;
                }

                this.error = "";
                this.message = "";
                try {
                    await this.request(`/api/v1/admin/users/${item.userId}/roles`, "PUT", { roleIds });
                    this.message = "角色分配成功";
                    await this.loadUsers();
                } catch (error) {
                    this.error = error instanceof Error ? error.message : "角色分配失败";
                }
            },
            changePage(page) {
                if (page < 1 || page > this.totalPages) return;
                this.pageIndex = page;
                this.loadUsers();
            },
            formatDate(value) {
                return value ? new Date(value).toLocaleString() : "-";
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
    }).mount("#adminUsersApp");
})();
