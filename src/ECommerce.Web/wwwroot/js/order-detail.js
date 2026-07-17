(function () {
    const { createApp } = Vue;
    const orderId = window.__ORDER_ID || 0;

    createApp({
        data() {
            return {
                loading: false,
                order: null,
                logistics: null,
                logisticsLoading: false,
                reviewForm: null,
                submittingReview: false
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
                        if (this.order.status === 2 || this.order.status === 3) {
                            await this.loadLogistics();
                        } else {
                            this.logistics = null;
                        }
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

            async loadLogistics() {
                this.logisticsLoading = true;
                try {
                    const response = await fetch(`/api/v1/logistics/${orderId}`, { headers: { 'Accept': 'application/json' } });
                    const result = await response.json();
                    this.logistics = response.ok && result.success ? result.data : null;
                } catch (error) {
                    console.error('加载物流失败:', error);
                    this.logistics = null;
                } finally {
                    this.logisticsLoading = false;
                }
            },

            logisticsStatusText(status) {
                return ({ 0: '已揽收', 1: '运输中', 2: '派送中', 3: '已签收' })[status] || '未知';
            },

            openReview(item) {
                this.reviewForm = {
                    productId: item.productId,
                    productName: item.productName,
                    rating: 5,
                    content: '',
                    imageUrls: '',
                    isAnonymous: false
                };
            },

            async submitReview() {
                if (!this.reviewForm) return;
                this.submittingReview = true;
                const images = this.reviewForm.imageUrls.split(/\r?\n/).map(value => value.trim()).filter(Boolean);
                try {
                    const response = await fetch('/api/v1/reviews', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
                        body: JSON.stringify({
                            orderId,
                            productId: this.reviewForm.productId,
                            rating: this.reviewForm.rating,
                            content: this.reviewForm.content || null,
                            images,
                            isAnonymous: this.reviewForm.isAnonymous
                        })
                    });
                    const result = await response.json();
                    if (!response.ok || !result.success) throw new Error(result.message || '评价提交失败');
                    alert('评价已提交，审核通过后将在商品页展示。');
                    this.reviewForm = null;
                } catch (error) {
                    alert(error.message || '评价提交失败');
                } finally {
                    this.submittingReview = false;
                }
            },

            goPay(orderId) {
                window.location.href = `/payment/${orderId}`;
            }
        }
    }).mount('#orderDetailApp');
})();
