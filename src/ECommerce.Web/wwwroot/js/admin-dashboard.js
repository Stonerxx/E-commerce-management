(function () {
    const { createApp } = Vue;

    createApp({
        data() {
            return {
                loading: false,
                summaryCards: [
                    {
                        key: "products",
                        label: "商品管理",
                        value: "已接入",
                        badge: "member3",
                        badgeClass: "text-bg-info",
                        hint: "分类、商品、SKU 和库存页面已合入预演分支"
                    },
                    {
                        key: "orders",
                        label: "订单管理",
                        value: "已接入",
                        badge: "member4",
                        badgeClass: "text-bg-warning",
                        hint: "购物车和订单核心流程来自 member4"
                    },
                    {
                        key: "auth",
                        label: "用户权限",
                        value: "已接入",
                        badge: "member2",
                        badgeClass: "text-bg-success",
                        hint: "登录、地址、权限和操作日志来自 member2"
                    },
                    {
                        key: "warnings",
                        label: "库存预警",
                        value: "待联调",
                        badge: "库存",
                        badgeClass: "text-bg-danger",
                        hint: "需要真实数据库数据后检查库存预警接口"
                    }
                ],
                moduleProgress: [
                    {
                        name: "项目骨架与部署",
                        branch: "feat-member1-foundation-oracle-deploy",
                        percent: 80,
                        barClass: "bg-primary",
                        nextStep: "确认 GitHub Actions 部署和服务器环境变量"
                    },
                    {
                        name: "用户、权限、地址、日志",
                        branch: "feat-member2-user-permission-address-log",
                        percent: 75,
                        barClass: "bg-success",
                        nextStep: "确认 demo 账号 password_hash 或保留 TEMP_DEMO_AUTH"
                    },
                    {
                        name: "商品、分类、SKU、库存",
                        branch: "feat-member3-product-category-sku-inventory",
                        percent: 70,
                        barClass: "bg-info",
                        nextStep: "确认商品 seed 数据和库存日志类型"
                    },
                    {
                        name: "购物车、订单核心流程",
                        branch: "feat-member4-cart-order-core",
                        percent: 70,
                        barClass: "bg-warning",
                        nextStep: "继续联调支付、优惠券、库存扣减"
                    },
                    {
                        name: "支付、优惠券、物流、评价",
                        branch: "feat-member5-payment-coupon-logistics-review",
                        percent: 20,
                        barClass: "bg-danger",
                        nextStep: "合入后替换 TEMP_DEMO_PAYMENT 和 MockCouponService"
                    },
                    {
                        name: "统计、导出、UI、文档",
                        branch: "feat-member6-stats-export-ui-docs",
                        percent: 20,
                        barClass: "bg-secondary",
                        nextStep: "合入后统一 Dashboard 和统计导出入口"
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
                    },
                    {
                        url: "/api/health",
                        status: "未检查",
                        badgeClass: "text-bg-secondary",
                        message: "member3 兼容健康检查入口"
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

                    item.status = response.ok ? "正常" : "异常";
                    item.badgeClass = response.ok ? "text-bg-success" : "text-bg-danger";
                    item.message = payload.message || payload.status || JSON.stringify(payload).slice(0, 80);
                } catch (error) {
                    item.status = "异常";
                    item.badgeClass = "text-bg-danger";
                    item.message = error.message;
                }
            }
        }
    }).mount("#dashboardApp");
})();
