(function () {
    const { createApp } = Vue;

    createApp({
        data() {
            // 默认时间范围：最近30天
            const now = new Date();
            const thirtyDaysAgo = new Date(now);
            thirtyDaysAgo.setDate(thirtyDaysAgo.getDate() - 30);

            return {
                loading: false,
                errorMessage: "",
                lastUpdated: "尚未加载",
                // ===== 新增：筛选条件 =====
                filters: {
                    startDate: this.formatDateInput(thirtyDaysAgo),
                    endDate: this.formatDateInput(now),
                    dimension: "day",
                    status: null // null 表示全部，数字对应 OrderStatus 枚举
                },
                summaryCards: [
                    {
                        key: "orders",
                        label: "今日订单",
                        value: "0",
                        icon: "fa-solid fa-bag-shopping",
                        badgeClass: "text-bg-primary",
                        hint: "来自 ORDER_STAT_SNAPSHOT"
                    },
                    {
                        key: "sales",
                        label: "今日销售额",
                        value: "¥0.00",
                        icon: "fa-solid fa-yen-sign",
                        badgeClass: "text-bg-success",
                        hint: "已支付销售额快照"
                    },
                    {
                        key: "shipments",
                        label: "待发货",
                        value: "0",
                        icon: "fa-solid fa-truck",
                        badgeClass: "text-bg-warning",
                        hint: "已支付待发货订单"
                    },
                    {
                        key: "warnings",
                        label: "库存预警",
                        value: "0",
                        icon: "fa-solid fa-triangle-exclamation",
                        badgeClass: "text-bg-danger",
                        hint: "可用库存低于预警线"
                    },
                    {
                        key: "reviews",
                        label: "待审核评价",
                        value: "0",
                        icon: "fa-solid fa-star",
                        badgeClass: "text-bg-secondary",
                        hint: "评价审核队列"
                    }
                ],
                topProducts: [],
                trendData: {
                    dates: [],
                    orderCounts: [],
                    salesAmounts: []
                },
                chartInstance: null
            };
        },
        mounted() {
            this.refreshAll();
            window.addEventListener("resize", this.resizeChart);
        },
        beforeUnmount() {
            window.removeEventListener("resize", this.resizeChart);
            if (this.chartInstance) {
                this.chartInstance.dispose();
                this.chartInstance = null;
            }
        },
        methods: {
            // ===== 刷新所有数据（使用当前筛选条件） =====
            async refreshAll() {
                this.loading = true;
                this.errorMessage = "";

                try {
                    await Promise.all([
                        this.loadDashboardSummary(),
                        this.loadTopProducts(),
                        this.loadTrendData()
                    ]);
                    this.lastUpdated = new Date().toLocaleString();
                } catch (error) {
                    this.errorMessage = error.message || "加载统计数据失败";
                } finally {
                    this.loading = false;
                }
            },

            // ===== 点击“查询”按钮时调用 =====
            async applyFilters() {
                await this.refreshAll();
            },

            // ===== 通用 API 请求方法 =====
            async apiGet(url) {
                const response = await fetch(url, { credentials: "same-origin" });
                const payload = await response.json().catch(() => null);

                if (!response.ok || !payload || !payload.success) {
                    throw new Error(payload?.message || `请求失败：${response.status}`);
                }

                return payload.data;
            },

            // ===== 加载五个卡片（不变） =====
            async loadDashboardSummary() {
                const data = await this.apiGet("/api/v1/admin/dashboard/summary");
                this.summaryCards[0].value = String(data?.todayOrderCount ?? 0);
                this.summaryCards[1].value = this.formatMoney(data?.todaySalesAmount ?? 0);
                this.summaryCards[2].value = String(data?.pendingShipmentCount ?? 0);
                this.summaryCards[3].value = String(data?.inventoryWarningCount ?? 0);
                this.summaryCards[4].value = String(data?.pendingReviewCount ?? 0);
            },

            // ===== 加载热销商品（使用筛选条件） =====
            async loadTopProducts() {
                const params = this.buildFilterParams();
                const data = await this.apiGet(`/api/v1/admin/statistics/top-products?${params}`);
                this.topProducts = data || [];
            },

            // ===== 加载趋势数据（使用筛选条件） =====
            async loadTrendData() {
                const params = this.buildFilterParams();
                const data = await this.apiGet(`/api/v1/admin/statistics/orders?${params}`);
                const points = data?.points || [];

                this.trendData.dates = points.map(point => this.formatShortDate(point.date));
                this.trendData.orderCounts = points.map(point => point.orderCount || 0);
                this.trendData.salesAmounts = points.map(point => Number(point.salesAmount || 0));
                this.renderChart();
            },

            // ===== 构建查询参数（核心：把 filters 转成 URL 参数） =====
            buildFilterParams() {
                const params = new URLSearchParams();
                if (this.filters.startDate) {
                    params.append("startDate", this.filters.startDate);
                }
                if (this.filters.endDate) {
                    params.append("endDate", this.filters.endDate);
                }
                if (this.filters.dimension) {
                    params.append("dimension", this.filters.dimension);
                }
                // 状态：如果选了具体状态，传数字；如果选了“全部”，不传（后端默认查全部）
                if (this.filters.status !== null && this.filters.status !== undefined) {
                    params.append("status", String(this.filters.status));
                }
                return params.toString();
            },

            // ===== 导出订单（核心：使用当前筛选条件） =====
            async exportOrders() {
                try {
                    const params = this.buildFilterParams();
                    const response = await fetch(`/api/v1/admin/exports/orders?${params}`, {
                        credentials: "same-origin"
                    });
                    const payload = await response.json();

                    if (!payload.success) {
                        this.errorMessage = payload.message || "导出失败";
                        return;
                    }

                    // 从 FileExportDto 中取数据
                    const fileData = payload.data;
                    // 将 Base64 字符串转为字节数组
                    const byteCharacters = atob(fileData.content);
                    const byteNumbers = new Array(byteCharacters.length);
                    for (let i = 0; i < byteCharacters.length; i++) {
                        byteNumbers[i] = byteCharacters.charCodeAt(i);
                    }
                    const byteArray = new Uint8Array(byteNumbers);
                    const blob = new Blob([byteArray], { type: fileData.contentType });

                    // 创建下载链接
                    const link = document.createElement("a");
                    link.href = URL.createObjectURL(blob);
                    link.download = fileData.fileName;
                    document.body.appendChild(link);
                    link.click();
                    document.body.removeChild(link);
                    URL.revokeObjectURL(link.href);
                } catch (error) {
                    this.errorMessage = `导出失败：${error.message}`;
                }
            }, 

            // ===== 渲染图表（不变） =====
            renderChart() {
                const dom = document.getElementById("trendChart");
                if (!dom) return;

                if (!this.chartInstance) {
                    this.chartInstance = echarts.init(dom);
                }

                const hasData = this.trendData.dates.length > 0;
                this.chartInstance.setOption({
                    tooltip: { trigger: "axis" },
                    legend: { top: 0 },
                    grid: {
                        left: "3%",
                        right: "4%",
                        bottom: "6%",
                        top: "14%",
                        containLabel: true
                    },
                    xAxis: {
                        type: "category",
                        data: hasData ? this.trendData.dates : ["暂无数据"]
                    },
                    yAxis: [
                        { type: "value", name: "订单数" },
                        { type: "value", name: "销售额" }
                    ],
                    series: [
                        {
                            name: "订单数",
                            type: "line",
                            smooth: true,
                            data: hasData ? this.trendData.orderCounts : [0]
                        },
                        {
                            name: "销售额",
                            type: "bar",
                            yAxisIndex: 1,
                            data: hasData ? this.trendData.salesAmounts : [0]
                        }
                    ]
                });
            },

            resizeChart() {
                if (this.chartInstance) {
                    this.chartInstance.resize();
                }
            },

            // ===== 工具方法（不变） =====
            formatMoney(value) {
                const amount = Number(value || 0);
                return `¥${amount.toFixed(2)}`;
            },

            formatDateInput(date) {
                return date.toISOString().slice(0, 10);
            },

            formatShortDate(value) {
                const date = new Date(value);
                if (Number.isNaN(date.getTime())) return "";
                return `${date.getMonth() + 1}/${date.getDate()}`;
            }
        }
    }).mount("#dashboardApp");
})();