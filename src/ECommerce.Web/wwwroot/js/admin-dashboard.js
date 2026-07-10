(function () {
    const { createApp } = Vue;

    createApp({
        data() {
            return {
                loading: false,
                lastUpdated: '加载中...',
                summaryCards: [
                    {
                        key: "orders",
                        label: "今日订单",
                        value: "0",
                        icon: '<i class="fas fa-shopping-bag"></i>'
                    },
                    {
                        key: "sales",
                        label: "今日销售额",
                        value: "¥0.00",
                        icon: '<i class="fas fa-yen-sign"></i>'
                    },
                    {
                        key: "shipments",
                        label: "待发货",
                        value: "0",
                        icon: '<i class="fas fa-truck"></i>'
                    },
                    {
                        key: "warnings",
                        label: "库存预警",
                        value: "0",
                        icon: '<i class="fas fa-exclamation-triangle"></i>'
                    },
                    {
                        key: "reviews",
                        label: "待审核评价",
                        value: "0",
                        icon: '<i class="fas fa-star"></i>'
                    }
                ],
                topProducts: [],
                trendData: {
                    dates: [],
                    orderCounts: []
                },
                chartInstance: null
            };
        },
        mounted() {
            this.loadDashboardSummary();
            this.loadTopProducts();
            this.loadTrendData();
            this.updateLastUpdated();
            // 窗口大小变化时重绘图表
            window.addEventListener('resize', this.resizeChart);
        },
        beforeUnmount() {
            window.removeEventListener('resize', this.resizeChart);
            if (this.chartInstance) {
                this.chartInstance.dispose();
                this.chartInstance = null;
            }
        },
        methods: {
            updateLastUpdated() {
                const now = new Date();
                this.lastUpdated = `${now.toLocaleDateString()} ${now.toLocaleTimeString()}`;
            },

            async refreshAll() {
                this.loading = true;
                await Promise.all([
                    this.loadDashboardSummary(true),
                    this.loadTopProducts(true),
                    this.loadTrendData(true)
                ]);
                this.updateLastUpdated();
                this.loading = false;
            },

            async loadDashboardSummary(silent = false) {
                try {
                    const response = await fetch('/api/v1/admin/dashboard/summary');
                    const result = await response.json();
                    if (response.ok && result.success) {
                        const data = result.data;
                        this.summaryCards[0].value = data.todayOrderCount?.toString() || "0";
                        this.summaryCards[1].value = `¥${(data.todaySalesAmount ?? 0).toFixed(2)}`;
                        this.summaryCards[2].value = data.pendingShipmentCount?.toString() || "0";
                        this.summaryCards[3].value = data.inventoryWarningCount?.toString() || "0";
                        this.summaryCards[4].value = data.pendingReviewCount?.toString() || "0";
                    }
                } catch (e) {
                    if (!silent) console.error('加载统计摘要失败:', e);
                }
            },

            async loadTopProducts(silent = false) {
                try {
                    // 默认近30天
                    const end = new Date();
                    const start = new Date();
                    start.setDate(start.getDate() - 30);
                    const params = new URLSearchParams({
                        startDate: start.toISOString().split('T')[0],
                        endDate: end.toISOString().split('T')[0]
                    });
                    const response = await fetch(`/api/v1/admin/statistics/top-products?${params}`);
                    const result = await response.json();
                    if (response.ok && result.success) {
                        this.topProducts = result.data || [];
                    }
                } catch (e) {
                    if (!silent) console.error('加载热销商品失败:', e);
                }
            },

            async loadTrendData(silent = false) {
                try {
                    const end = new Date();
                    const start = new Date();
                    start.setDate(start.getDate() - 30);
                    const params = new URLSearchParams({
                        startDate: start.toISOString().split('T')[0],
                        endDate: end.toISOString().split('T')[0]
                    });
                    const response = await fetch(`/api/v1/admin/statistics/orders?${params}`);
                    const result = await response.json();
                    if (response.ok && result.success && result.data) {
                        const points = result.data.points || [];
                        this.trendData.dates = points.map(p => {
                            const d = new Date(p.date);
                            return `${d.getMonth() + 1}/${d.getDate()}`;
                        });
                        this.trendData.orderCounts = points.map(p => p.orderCount);
                        this.renderChart();
                    }
                } catch (e) {
                    if (!silent) console.error('加载趋势数据失败:', e);
                }
            },

            renderChart() {
                const dom = document.getElementById('trendChart');
                if (!dom) return;
                if (this.chartInstance) {
                    this.chartInstance.dispose();
                }
                this.chartInstance = echarts.init(dom);
                const option = {
                    tooltip: {
                        trigger: 'axis',
                        axisPointer: { type: 'shadow' }
                    },
                    grid: {
                        left: '3%',
                        right: '4%',
                        bottom: '8%',
                        top: '6%',
                        containLabel: true
                    },
                    xAxis: {
                        type: 'category',
                        data: this.trendData.dates.length > 0 ? this.trendData.dates : ['暂无数据'],
                        axisLine: { show: false },
                        axisTick: { show: false }
                    },
                    yAxis: {
                        type: 'value',
                        splitLine: { lineStyle: { color: '#f0f0f0' } },
                        axisLabel: { fontSize: 11 }
                    },
                    series: [
                        {
                            name: '订单数',
                            type: 'line',
                            smooth: true,
                            symbol: 'circle',
                            symbolSize: 6,
                            lineStyle: {
                                width: 2,
                                color: '#4f6f8f'
                            },
                            areaStyle: {
                                color: {
                                    type: 'linear',
                                    x: 0, y: 0, x2: 0, y2: 1,
                                    colorStops: [
                                        { offset: 0, color: 'rgba(79,111,143,0.3)' },
                                        { offset: 1, color: 'rgba(79,111,143,0.02)' }
                                    ]
                                }
                            },
                            data: this.trendData.orderCounts.length > 0 ? this.trendData.orderCounts : [0]
                        }
                    ]
                };
                this.chartInstance.setOption(option);
            },

            resizeChart() {
                if (this.chartInstance) {
                    this.chartInstance.resize();
                }
            }
        }
    }).mount("#dashboardApp");
})();