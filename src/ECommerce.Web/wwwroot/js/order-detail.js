(function () {
    const { createApp } = Vue;
    const orderId = window.__ORDER_ID || 0;

    createApp({
        data() {
            return {
                loading: false,
                order: null
            };
        },
        mounted() {
            if (orderId > 0) {
                this.loadOrderDetail();
            }
        },
        methods: {
            async loadOrderDetail() {
                this.loading = true;
                try {
                    const response = await fetch(`/api/v1/orders/${orderId}`, {
                        headers: { 'Accept': 'application/json' }
                    });
                    const result = await response.json();

                    if (result.success && result.data) {
                        this.order = result.data;
                    } else {
                        console.error('加载订单详情失败:', result.message);
                    }
                } catch (error) {
                    console.error('加载订单详情异常:', error);
                } finally {
                    this.loading = false;
                }
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
                    0: 'text-bg-warning',
                    1: 'text-bg-info',
                    2: 'text-bg-primary',
                    3: 'text-bg-success',
                    4: 'text-bg-secondary'
                };
                return map[status] || 'text-bg-secondary';
            },

            formatDate(dateStr) {
                if (!dateStr) return '-';
                const date = new Date(dateStr);
                return date.toLocaleString('zh-CN');
            },

            parseReceiver(jsonStr) {
                if (!jsonStr) return '无收货信息';
                try {
                    const data = JSON.parse(jsonStr);
                    return `${data.receiverName}，${data.receiverPhone}，${data.province}${data.city}${data.district}${data.detailAddress}`;
                } catch {
                    return jsonStr;
                }
            },

            getLogDescription(log) {
                const statusMap = {
                    0: '待支付',
                    1: '已支付',
                    2: '已发货',
                    3: '已完成',
                    4: '已取消'
                };
                const from = log.fromStatus !== null && log.fromStatus !== undefined
                    ? statusMap[log.fromStatus] || log.fromStatus
                    : '无';
                const to = statusMap[log.toStatus] || log.toStatus;
                return `${from} → ${to}${log.remark ? '（' + log.remark + '）' : ''}`;
            },

            async cancelOrder(orderId) {
                if (!confirm('确定要取消这个订单吗？')) return;

                try {
                    const response = await fetch(`/api/v1/orders/${orderId}/cancel`, {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'Accept': 'application/json'
                        },
                        body: JSON.stringify({ reason: '用户主动取消' })
                    });
                    const result = await response.json();

                    if (result.success) {
                        this.loadOrderDetail();
                    } else {
                        alert(result.message || '取消订单失败');
                    }
                } catch (error) {
                    console.error('取消订单异常:', error);
                    alert('取消订单失败，请稍后重试');
                }
            },

            async confirmOrder(orderId) {
                if (!confirm('确认已收到商品？')) return;

                try {
                    const response = await fetch(`/api/v1/orders/${orderId}/confirm`, {
                        method: 'POST',
                        headers: { 'Accept': 'application/json' }
                    });
                    const result = await response.json();

                    if (result.success) {
                        this.loadOrderDetail();
                    } else {
                        alert(result.message || '确认收货失败');
                    }
                } catch (error) {
                    console.error('确认收货异常:', error);
                    alert('确认收货失败，请稍后重试');
                }
            },

            goPay(orderId) {
                window.location.href = `/payment/${orderId}`;
            }
        }
    }).mount('#orderDetailApp');
})();
