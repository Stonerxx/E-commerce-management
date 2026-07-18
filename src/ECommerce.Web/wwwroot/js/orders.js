(function () {
    const { createApp } = Vue;

    createApp({
        data() {
            return {
                loading: true,
                errorMessage: "",
                orders: [],
                pageIndex: 1,
                pageSize: 10,
                totalCount: 0,
                totalPages: 1,
                busyOrderIds: [],
                filters: { status: "", startTime: "", endTime: "" }
            };
        },
        computed: {
            visiblePages() {
                const pages = [];
                let start = Math.max(1, this.pageIndex - 2);
                let end = Math.min(this.totalPages, start + 4);
                if (end - start < 4) start = Math.max(1, end - 4);
                for (let page = start; page <= end; page += 1) pages.push(page);
                return pages;
            }
        },
        mounted() {
            this.loadOrders();
        },
        methods: {
            async loadOrders() {
                this.loading = true;
                this.errorMessage = "";
                try {
                    const params = new URLSearchParams({ pageIndex: String(this.pageIndex), pageSize: String(this.pageSize) });
                    if (this.filters.status !== "") params.set("status", this.filters.status);
                    if (this.filters.startTime) params.set("startTime", `${this.filters.startTime}T00:00:00`);
                    if (this.filters.endTime) params.set("endTime", `${this.filters.endTime}T23:59:59`);

                    const payload = await this.request(`/api/v1/orders?${params}`);
                    this.orders = payload.data?.items || [];
                    this.totalCount = payload.data?.totalCount || 0;
                    this.totalPages = Math.max(1, payload.data?.totalPages || 1);
                    this.pageIndex = Math.min(this.pageIndex, this.totalPages);
                } catch (error) {
                    this.orders = [];
                    this.totalCount = 0;
                    this.errorMessage = error instanceof Error ? error.message : "订单加载失败";
                } finally {
                    this.loading = false;
                }
            },
            applyFilters() {
                if (this.filters.startTime && this.filters.endTime && this.filters.startTime > this.filters.endTime) {
                    window.appToast?.("开始日期不能晚于结束日期", "warning");
                    return;
                }
                this.pageIndex = 1;
                this.loadOrders();
            },
            resetFilters() {
                this.filters = { status: "", startTime: "", endTime: "" };
                this.pageIndex = 1;
                this.loadOrders();
            },
            goPage(page) {
                if (page < 1 || page > this.totalPages || page === this.pageIndex) return;
                this.pageIndex = page;
                this.loadOrders();
                window.scrollTo({ top: 0, behavior: "smooth" });
            },
            isBusy(orderId) {
                return this.busyOrderIds.includes(orderId);
            },
            setBusy(orderId, busy) {
                this.busyOrderIds = busy ? [...new Set([...this.busyOrderIds, orderId])] : this.busyOrderIds.filter(id => id !== orderId);
            },
            getStatusText(status) {
                return ({ 0: "待支付", 1: "已支付", 2: "已发货", 3: "已完成", 4: "已取消" })[Number(status)] || "未知";
            },
            getStatusBadge(status) {
                return ({ 0: "text-bg-warning", 1: "text-bg-info", 2: "text-bg-primary", 3: "text-bg-success", 4: "text-bg-secondary" })[Number(status)] || "text-bg-secondary";
            },
            formatDate(value) {
                if (!value) return "-";
                const date = new Date(value);
                return Number.isNaN(date.getTime()) ? "-" : date.toLocaleString("zh-CN");
            },
            formatMoney(value) {
                return Number(value || 0).toFixed(2);
            },
            async cancelOrder(order) {
                if (!window.confirm(`确认取消订单 ${order.orderNo} 吗？`)) return;
                this.setBusy(order.orderId, true);
                try {
                    await this.request(`/api/v1/orders/${order.orderId}/cancel`, "POST", { reason: "用户主动取消" });
                    window.appToast?.("订单已取消，库存将自动释放", "success");
                    await this.loadOrders();
                } catch (error) {
                    window.appToast?.(error instanceof Error ? error.message : "取消订单失败", "danger");
                } finally {
                    this.setBusy(order.orderId, false);
                }
            },
            async confirmOrder(order) {
                if (!window.confirm(`确认已收到订单 ${order.orderNo} 的商品吗？`)) return;
                this.setBusy(order.orderId, true);
                try {
                    await this.request(`/api/v1/orders/${order.orderId}/confirm`, "POST");
                    window.appToast?.("已确认收货", "success");
                    await this.loadOrders();
                } catch (error) {
                    window.appToast?.(error instanceof Error ? error.message : "确认收货失败", "danger");
                } finally {
                    this.setBusy(order.orderId, false);
                }
            },
            goPay(orderId) {
                window.location.href = `/payment/${orderId}`;
            },
            async request(url, method = "GET", body = null) {
                const response = await fetch(url, {
                    method,
                    headers: { Accept: "application/json", ...(body ? { "Content-Type": "application/json" } : {}) },
                    body: body ? JSON.stringify(body) : null
                });
                const payload = await response.json().catch(() => null);
                if (!response.ok || !payload?.success) throw new Error(payload?.message || `请求失败（${response.status}）`);
                return payload;
            }
        }
    }).mount("#ordersApp");
})();
