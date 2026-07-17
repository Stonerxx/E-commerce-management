(function () {
    const { createApp } = Vue;
    const formatDateInput = date => {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, "0");
        const day = String(date.getDate()).padStart(2, "0");
        return `${year}-${month}-${day}`;
    };

    createApp({
        data() {
            const endDate = new Date();
            const startDate = new Date();
            startDate.setDate(startDate.getDate() - 29);

            return {
                loading: false,
                errorMessage: "",
                filters: {
                    startDate: formatDateInput(startDate),
                    endDate: formatDateInput(endDate),
                    dimension: "day"
                },
                report: {
                    points: [],
                    orderCount: 0,
                    paidCount: 0,
                    salesAmount: 0,
                    avgOrderAmount: 0
                },
                topProducts: [],
                chartInstance: null
            };
        },
        computed: {
            rangeLabel() {
                const dimension = this.filters.dimension === "month" ? "按月" : "按日";
                return `${this.filters.startDate} 至 ${this.filters.endDate} · ${dimension}`;
            }
        },
        mounted() {
            this.loadReport();
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
            async loadReport() {
                if (!this.validateRange()) {
                    return;
                }

                this.loading = true;
                this.errorMessage = "";

                try {
                    const params = this.buildStatisticsParams();
                    const [report, topProducts] = await Promise.all([
                        this.apiGet(`/api/v1/admin/statistics/orders?${params}`),
                        this.apiGet(`/api/v1/admin/statistics/top-products?${params}`)
                    ]);

                    this.report = {
                        points: report?.points || [],
                        orderCount: report?.orderCount || 0,
                        paidCount: report?.paidCount || 0,
                        salesAmount: Number(report?.salesAmount || 0),
                        avgOrderAmount: Number(report?.avgOrderAmount || 0)
                    };
                    this.topProducts = topProducts || [];
                    this.renderChart();
                } catch (error) {
                    this.errorMessage = error.message || "加载统计报表失败";
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

            setRange(days, dimension = "day") {
                const endDate = new Date();
                const startDate = new Date();
                startDate.setDate(startDate.getDate() - days + 1);
                this.filters.startDate = this.formatDateInput(startDate);
                this.filters.endDate = this.formatDateInput(endDate);
                this.filters.dimension = dimension;
                this.loadReport();
            },

            exportOrders() {
                if (!this.validateRange()) {
                    return;
                }

                const params = this.buildExportParams();
                window.location.assign(`/api/v1/admin/exports/orders?${params}`);
            },

            exportInventory() {
                if (!this.validateRange()) {
                    return;
                }

                const params = this.buildExportParams();
                window.location.assign(`/api/v1/admin/exports/inventory?${params}`);
            },

            buildStatisticsParams() {
                return new URLSearchParams({
                    startDate: this.filters.startDate,
                    endDate: this.filters.endDate,
                    dimension: this.filters.dimension
                });
            },

            buildExportParams() {
                return new URLSearchParams({
                    startTime: this.filters.startDate,
                    endTime: this.filters.endDate,
                    pageSize: "5000"
                });
            },

            validateRange() {
                if (!this.filters.startDate || !this.filters.endDate) {
                    this.errorMessage = "请选择完整的开始和结束日期";
                    return false;
                }

                if (this.filters.startDate > this.filters.endDate) {
                    this.errorMessage = "结束日期不能早于开始日期";
                    return false;
                }

                this.errorMessage = "";
                return true;
            },

            renderChart() {
                const chartElement = document.getElementById("statisticsTrendChart");
                if (!chartElement) {
                    return;
                }

                if (!this.chartInstance) {
                    this.chartInstance = echarts.init(chartElement);
                }

                const points = this.report.points;
                const labels = points.length > 0
                    ? points.map(point => this.formatPeriod(point.date))
                    : ["暂无数据"];

                this.chartInstance.setOption({
                    tooltip: { trigger: "axis" },
                    legend: { top: 0 },
                    grid: { left: "3%", right: "4%", bottom: "6%", top: "14%", containLabel: true },
                    xAxis: { type: "category", data: labels },
                    yAxis: [
                        { type: "value", name: "订单数", minInterval: 1 },
                        { type: "value", name: "销售额" }
                    ],
                    series: [
                        {
                            name: "订单数",
                            type: "line",
                            smooth: true,
                            data: points.length > 0 ? points.map(point => point.orderCount || 0) : [0]
                        },
                        {
                            name: "已支付",
                            type: "line",
                            smooth: true,
                            data: points.length > 0 ? points.map(point => point.paidCount || 0) : [0]
                        },
                        {
                            name: "销售额",
                            type: "bar",
                            yAxisIndex: 1,
                            data: points.length > 0 ? points.map(point => Number(point.salesAmount || 0)) : [0]
                        }
                    ]
                });
            },

            resizeChart() {
                this.chartInstance?.resize();
            },

            formatDateInput(date) {
                return formatDateInput(date);
            },

            formatPeriod(value) {
                const date = new Date(value);
                if (Number.isNaN(date.getTime())) {
                    return "-";
                }

                const year = date.getFullYear();
                const month = String(date.getMonth() + 1).padStart(2, "0");
                if (this.filters.dimension === "month") {
                    return `${year}-${month}`;
                }

                const day = String(date.getDate()).padStart(2, "0");
                return `${year}-${month}-${day}`;
            },

            formatMoney(value) {
                return `¥${Number(value || 0).toFixed(2)}`;
            }
        }
    }).mount("#statisticsApp");
})();
