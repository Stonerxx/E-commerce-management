(function () {
    const { createApp } = Vue;

    createApp({
        data() {
            return {
                loading: false,
                orders: [],
                pageIndex: 1,
                pageSize: 10,
                totalCount: 0,
                totalPages: 1,
                filters: {
                    orderNo: '',
                    userId: null,
                    status: '',
                    startTime: '',
                    endTime: ''
                },
                statusCounts: {},
                shipping: false,
                shipModal: null,
                shipment: {
                    orderId: null,
                    companyName: '',
                    trackingNo: ''
                }
            };
        },
        computed: {
            visiblePages() {
                const pages = [];
                const maxVisible = 5;
                let start = Math.max(1, this.pageIndex - Math.floor(maxVisible / 2));
                let end = Math.min(this.totalPages, start + maxVisible - 1);
                if (end - start < maxVisible - 1) {
                    start = Math.max(1, end - maxVisible + 1);
                }
                for (let i = start; i <= end; i++) {
                    pages.push(i);
                }
                return pages;
            }
        },
        mounted() {
            this.shipModal = bootstrap.Modal.getOrCreateInstance(this.$refs.shipModalElement);
            this.$refs.shipModalElement.addEventListener('shown.bs.modal', () => {
                this.$refs.shipmentCompanyInput?.focus();
            });
            this.loadOrders();
        },
        beforeUnmount() {
            this.shipModal?.dispose();
        },
        methods: {
            buildFilterParams(includePagination = false) {
                const params = new URLSearchParams();
                if (includePagination) {
                    params.set('pageIndex', this.pageIndex);
                    params.set('pageSize', this.pageSize);
                }
                if (this.filters.orderNo) params.append('orderNo', this.filters.orderNo);
                if (this.filters.userId) params.append('userId', this.filters.userId);
                if (this.filters.status !== '') params.append('status', this.filters.status);
                if (this.filters.startTime) params.append('startTime', this.filters.startTime);
                if (this.filters.endTime) params.append('endTime', this.filters.endTime);
                return params;
            },

            async loadOrders() {
                this.loading = true;
                try {
                    const params = this.buildFilterParams(true);

                    const response = await fetch(`/api/v1/admin/orders?${params.toString()}`, {
                        headers: { 'Accept': 'application/json' }
                    });
                    const result = await response.json();

                    if (result.success && result.data) {
                        this.orders = result.data.items || [];
                        this.totalCount = result.data.totalCount || 0;
                        this.totalPages = result.data.totalPages || 1;
                        this.calculateStatusCounts();
                    } else {
                        console.error('加载订单失败:', result.message);
                    }
                } catch (error) {
                    console.error('加载订单异常:', error);
                } finally {
                    this.loading = false;
                }
            },

            applyFilters() {
                if (this.filters.startTime && this.filters.endTime
                    && new Date(this.filters.startTime) > new Date(this.filters.endTime)) {
                    window.appToast?.('开始时间不能晚于结束时间', 'warning');
                    return;
                }

                this.pageIndex = 1;
                this.loadOrders();
            },

            resetFilters() {
                this.filters = {
                    orderNo: '',
                    userId: null,
                    status: '',
                    startTime: '',
                    endTime: ''
                };
                this.pageIndex = 1;
                this.loadOrders();
            },

            exportOrders() {
                const params = this.buildFilterParams();
                window.location.assign(`/api/v1/admin/exports/orders?${params.toString()}`);
            },

            calculateStatusCounts() {
                this.statusCounts = {};
                this.orders.forEach(order => {
                    const status = order.status;
                    this.statusCounts[status] = (this.statusCounts[status] || 0) + 1;
                });
            },

            goPage(page) {
                if (page < 1 || page > this.totalPages) return;
                this.pageIndex = page;
                this.loadOrders();
            },

            getStatusText(status) {
                const map = {
                    0: '待支付',
                    1: '已支付',
                    2: '已发货',
                    3: '已完成',
                    4: '已取消'
                };
                return map[status] || '未知';
            },

            getStatusBadge(status) {
                const map = {
                    0: 'bg-warning text-dark',    // 待支付：黄底黑字
                    1: 'bg-info text-white',      // 已支付：蓝底白字（或 'bg-info text-dark' 更清晰）
                    2: 'bg-primary text-white',   // 已发货：深蓝底白字
                    3: 'bg-success text-white',   // 已完成：绿底白字
                    4: 'bg-secondary text-white'  // 已取消：灰底白字
                };
                return map[status] || 'bg-secondary text-white';
            },

            formatDate(dateStr) {
                if (!dateStr) return '-';
                const date = new Date(dateStr);
                return date.toLocaleString('zh-CN');
            },

            openShipModal(orderId) {
                this.shipment.orderId = orderId;
                this.shipment.companyName = '';
                this.shipment.trackingNo = '';
                this.shipModal?.show();
            },

            async submitShipment() {
                if (!this.shipment.companyName || !this.shipment.trackingNo) {
                    alert('请完整填写物流公司和运单号');
                    return;
                }

                this.shipping = true;
                try {
                    const response = await fetch(`/api/v1/admin/orders/${this.shipment.orderId}/shipments`, {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'Accept': 'application/json'
                        },
                        body: JSON.stringify({
                            companyName: this.shipment.companyName,
                            trackingNo: this.shipment.trackingNo,
                            shippedAt: new Date().toISOString()
                        })
                    });
                    const result = await response.json();

                    if (response.ok && result.success) {
                        this.shipModal?.hide();
                        window.appToast?.('发货成功，物流信息已创建', 'success');
                        await this.loadOrders();
                    } else {
                        window.appToast?.(result.message || '发货失败', 'danger');
                    }
                } catch (error) {
                    console.error('发货异常:', error);
                    window.appToast?.('发货失败，请稍后重试', 'danger');
                } finally {
                    this.shipping = false;
                }
            },

            async adminCancelOrder(orderId) {
                if (!confirm('确定要取消这个待支付订单吗？库存和优惠券将自动恢复。')) return;

                try {
                    const response = await fetch(`/api/v1/admin/orders/${orderId}/cancel`, {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'Accept': 'application/json'
                        },
                        body: JSON.stringify({ reason: '后台取消' })
                    });
                    const result = await response.json();

                    if (response.ok && result.success) {
                        window.appToast?.('订单已取消，相关资源已恢复', 'success');
                        await this.loadOrders();
                    } else {
                        window.appToast?.(result.message || '取消订单失败', 'danger');
                    }
                } catch (error) {
                    console.error('取消订单异常:', error);
                    window.appToast?.('取消订单失败，请稍后重试', 'danger');
                }
            },

            shipOrder(orderId) {
                this.openShipModal(orderId);
            }
        }
    }).mount('#adminOrdersApp');
})();
