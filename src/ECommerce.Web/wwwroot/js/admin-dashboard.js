(function () {
    const { createApp } = Vue;
    const dashboardElement = document.getElementById("dashboardApp");
    const isAdmin = dashboardElement?.dataset.isAdmin === "true";

    createApp({
        data() {
            return {
                loading: false,
                isAdmin,
                errorMessage: "",
                lastUpdated: "尚未加载",
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
                trendDimension: "day",
                trendData: {
                    dates: [],
                    orderCounts: [],
                    salesAmounts: []
                },
                chartInstance: null,
                themeChangeHandler: null
            };
        },
        mounted() {
            this.refreshAll();
            window.addEventListener("resize", this.resizeChart);
            this.themeChangeHandler = () => this.renderChart();
            window.addEventListener("app:theme-changed", this.themeChangeHandler);
        },
        beforeUnmount() {
            window.removeEventListener("resize", this.resizeChart);
            if (this.themeChangeHandler) {
                window.removeEventListener("app:theme-changed", this.themeChangeHandler);
            }
            if (this.chartInstance) {
                this.chartInstance.dispose();
                this.chartInstance = null;
            }
        },
        methods: {
            async refreshAll() {
                this.loading = true;
                this.errorMessage = "";

                try {
                    const requests = [this.loadDashboardSummary()];
                    if (this.isAdmin) {
                        requests.push(this.loadTopProducts(), this.loadTrendData());
                    }
                    await Promise.all(requests);
                    this.lastUpdated = new Date().toLocaleString();
                } catch (error) {
                    this.errorMessage = error.message || "加载统计数据失败";
                } finally {
                    this.loading = false;
                }
            },

            async apiGet(url) {
                const response = await fetch(url, { credentials: "same-origin" });
                const payload = await response.json().catch(() => null);

                if (!response.ok || !payload || !payload.success) {
                    throw new Error(payload?.message || `请求失败：${response.status}`);
                }

                return payload.data;
            },

            async loadDashboardSummary() {
                const data = await this.apiGet("/api/v1/admin/dashboard/summary");
                this.summaryCards[0].value = String(data?.todayOrderCount ?? 0);
                this.summaryCards[1].value = this.formatMoney(data?.todaySalesAmount ?? 0);
                this.summaryCards[2].value = String(data?.pendingShipmentCount ?? 0);
                this.summaryCards[3].value = String(data?.inventoryWarningCount ?? 0);
                this.summaryCards[4].value = String(data?.pendingReviewCount ?? 0);
            },

            async loadTopProducts() {
                const params = this.buildRangeParams(30);
                const data = await this.apiGet(`/api/v1/admin/statistics/top-products?${params}`);
                this.topProducts = data || [];
            },

            async loadTrendData() {
                const days = this.trendDimension === "month" ? 365 : 30;
                const params = this.buildRangeParams(days, this.trendDimension);
                const data = await this.apiGet(`/api/v1/admin/statistics/orders?${params}`);
                const points = data?.points || [];

                this.trendData.dates = points.map(point => this.formatTrendDate(point.date));
                this.trendData.orderCounts = points.map(point => point.orderCount || 0);
                this.trendData.salesAmounts = points.map(point => Number(point.salesAmount || 0));
                this.renderChart();
            },

            buildRangeParams(days, dimension = "day") {
                const end = new Date();
                const start = new Date();
                start.setDate(start.getDate() - days);

                return new URLSearchParams({
                    startDate: this.formatDateInput(start),
                    endDate: this.formatDateInput(end),
                    dimension
                });
            },

            async setTrendDimension(dimension) {
                if (this.trendDimension === dimension) {
                    return;
                }

                this.trendDimension = dimension;
                this.loading = true;
                this.errorMessage = "";
                try {
                    await this.loadTrendData();
                    this.lastUpdated = new Date().toLocaleString();
                } catch (error) {
                    this.errorMessage = error.message || "加载统计数据失败";
                } finally {
                    this.loading = false;
                }
            },

            renderChart() {
                const dom = document.getElementById("trendChart");
                if (!dom) {
                    return;
                }

                if (!this.chartInstance) {
                    this.chartInstance = echarts.init(dom);
                }

                const hasData = this.trendData.dates.length > 0;
                const dark = window.appTheme?.current() === "dark";
                const textColor = dark ? "#a2a8bb" : "#687086";
                const gridColor = dark ? "#303344" : "#e2e5ef";
                this.chartInstance.setOption({
                    backgroundColor: "transparent",
                    color: [dark ? "#818cf8" : "#4f46e5", dark ? "#34d399" : "#059669"],
                    textStyle: { color: textColor },
                    tooltip: { trigger: "axis" },
                    legend: { top: 0, textStyle: { color: textColor } },
                    grid: {
                        left: "3%",
                        right: "4%",
                        bottom: "6%",
                        top: "14%",
                        containLabel: true
                    },
                    xAxis: {
                        type: "category",
                        data: hasData ? this.trendData.dates : ["暂无数据"],
                        axisLabel: { color: textColor },
                        axisLine: { lineStyle: { color: gridColor } }
                    },
                    yAxis: [
                        { type: "value", name: "订单数", nameTextStyle: { color: textColor }, axisLabel: { color: textColor }, splitLine: { lineStyle: { color: gridColor } } },
                        { type: "value", name: "销售额", nameTextStyle: { color: textColor }, axisLabel: { color: textColor }, splitLine: { show: false } }
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
                }, true);
            },

            resizeChart() {
                if (this.chartInstance) {
                    this.chartInstance.resize();
                }
            },

            formatMoney(value) {
                const amount = Number(value || 0);
                return `¥${amount.toFixed(2)}`;
            },

            formatDateInput(date) {
                return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}`;
            },

            formatTrendDate(value) {
                const date = new Date(value);
                if (Number.isNaN(date.getTime())) {
                    return "";
                }

                return this.trendDimension === "month"
                    ? `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}`
                    : `${date.getMonth() + 1}/${date.getDate()}`;
            }
        },
        computed: {
            trendTitle() {
                return this.trendDimension === "month" ? "近 12 个月订单趋势" : "近 30 日订单趋势";
            }
        }
    }).mount("#dashboardApp");
})();
