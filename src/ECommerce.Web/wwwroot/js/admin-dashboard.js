(function () {
    const { createApp } = Vue;

    createApp({
        data() {
            return {
                loading: false,
                summaryCards: [
                    {
                        key: "orders",
                        label: "今日订单",
                        value: "0",
                        badge: "待实现",
                        badgeClass: "text-bg-secondary",
                        hint: "第 4 人实现订单后接入 /api/v1/admin/dashboard/summary"
                    },
                    {
                        key: "sales",
                        label: "今日销售额",
                        value: "¥0.00",
                        badge: "统计",
                        badgeClass: "text-bg-info",
                        hint: "第 6 人实现统计口径后替换示例数据"
                    },
                    {
                        key: "shipments",
                        label: "待发货",
                        value: "0",
                        badge: "物流",
                        badgeClass: "text-bg-warning",
                        hint: "第 5 人实现发货和物流轨迹"
                    },
                    {
                        key: "warnings",
                        label: "库存预警",
                        value: "0",
                        badge: "库存",
                        badgeClass: "text-bg-danger",
                        hint: "第 3 人实现库存预警列表"
                    }
                ],
                moduleProgress: [
                    {
                        name: "项目骨架与部署",
                        branch: "feat-member1-foundation-oracle-deploy",
                        percent: 55,
                        barClass: "bg-primary",
                        nextStep: "填真实 Oracle__ConnectionString，执行 db-check，补服务器访问和部署截图"
                    },
                    {
                        name: "用户、权限、地址、日志",
                        branch: "feat-member2-user-permission-address-log",
                        percent: 10,
                        barClass: "bg-success",
                        nextStep: "实现 AuthService，并让登录页提交后生成 Cookie"
                    },
                    {
                        name: "商品、分类、SKU、库存",
                        branch: "feat-member3-product-category-sku-inventory",
                        percent: 10,
                        barClass: "bg-info",
                        nextStep: "实现分类树、商品列表、SKU 和库存日志"
                    },
                    {
                        name: "购物车、订单核心流程",
                        branch: "feat-member4-cart-order-core",
                        percent: 10,
                        barClass: "bg-warning",
                        nextStep: "实现购物车转订单和库存锁定"
                    },
                    {
                        name: "支付、优惠券、物流、评价",
                        branch: "feat-member5-payment-coupon-logistics-review",
                        percent: 10,
                        barClass: "bg-danger",
                        nextStep: "实现模拟支付成功后的订单状态流转"
                    },
                    {
                        name: "统计、导出、UI、文档",
                        branch: "feat-member6-stats-export-ui-docs",
                        percent: 15,
                        barClass: "bg-secondary",
                        nextStep: "基于本 Dashboard 样板统一后台页面"
                    }
                ],
                apiChecks: [
                    {
                        url: "/health",
                        status: "未检查",
                        badgeClass: "text-bg-secondary",
                        message: "点击刷新系统状态后检查"
                    },
                    {
                        url: "/api/v1/system/version",
                        status: "未检查",
                        badgeClass: "text-bg-secondary",
                        message: "点击刷新系统状态后检查"
                    }
                ]
            };
        },
        mounted() {
            this.refreshSystemStatus();
        },
        methods: {
            async refreshSystemStatus() {
                this.loading = true;
                await Promise.all(this.apiChecks.map((item) => this.checkEndpoint(item)));
                this.loading = false;
            },
            async checkEndpoint(item) {
                try {
                    const response = await fetch(item.url, {
                        headers: {
                            "Accept": "application/json"
                        }
                    });
                    const payload = await response.json();

                    item.status = response.ok && payload.success ? "正常" : "异常";
                    item.badgeClass = response.ok && payload.success ? "text-bg-success" : "text-bg-danger";
                    item.message = payload.message || `HTTP ${response.status}`;
                } catch (error) {
                    item.status = "失败";
                    item.badgeClass = "text-bg-danger";
                    item.message = error instanceof Error ? error.message : "接口请求失败";
                }
            }
        }
    }).mount("#dashboardApp");
})();
