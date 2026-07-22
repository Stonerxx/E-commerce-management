(function () {
    const { createApp } = Vue;

    createApp({
        data() {
            return {
                loading: false,
                error: "",
                items: [],
                pageIndex: 1,
                pageSize: 10,
                totalCount: 0,
                totalPages: 0,
                filters: {
                    module: "",
                    operatorId: null,
                    startTime: "",
                    endTime: ""
                }
            };
        },
        mounted() {
            this.loadLogs();
        },
        methods: {
            async loadLogs() {
                this.loading = true;
                this.error = "";

                const params = new URLSearchParams();
                params.set("pageIndex", this.pageIndex.toString());
                params.set("pageSize", this.pageSize.toString());
                if (this.filters.module) params.set("module", this.filters.module);
                if (this.filters.operatorId) params.set("operatorId", this.filters.operatorId.toString());
                if (this.filters.startTime) params.set("startTime", this.filters.startTime);
                if (this.filters.endTime) params.set("endTime", this.filters.endTime);

                try {
                    const response = await fetch(`/api/v1/admin/operation-logs?${params.toString()}`, {
                        headers: {
                            "Accept": "application/json"
                        }
                    });
                    const payload = await response.json();
                    if (!response.ok || !payload.success) {
                        throw new Error(payload.message || `HTTP ${response.status}`);
                    }

                    this.items = payload.data.items || [];
                    this.pageIndex = payload.data.pageIndex;
                    this.pageSize = payload.data.pageSize;
                    this.totalCount = payload.data.totalCount;
                    this.totalPages = payload.data.totalPages;
                } catch (error) {
                    this.error = error instanceof Error ? error.message : "日志查询失败";
                } finally {
                    this.loading = false;
                }
            },
            changePage(page) {
                if (page < 1 || page > this.totalPages) {
                    return;
                }

                this.pageIndex = page;
                this.loadLogs();
            },
            formatDate(value) {
                return value ? new Date(value).toLocaleString() : "-";
            }
        }
    }).mount("#operationLogsApp");
})();
