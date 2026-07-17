(function () {
    const { createApp } = Vue;
    const orderId = window.__ORDER_ID || 0;

    createApp({
        data() {
            return {
                loading: false,
                order: null,
                showShipModal: false,
                showLogisticsPanel: false,
                logisticsLoading: false,
                logistics: null,
                trackForm: { trackDesc: '', location: '', status: 1 },
                shipment: {
                    companyName: '',
                    trackingNo: ''
                }
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
                    const response = await fetch(`/api/v1/admin/orders/${orderId}`, {
                        headers: { 'Accept': 'application/json' }
                    });
                    const result = await response.json();

                    if (result.success && result.data) {
                        this.order = result.data;
                        if (this.showLogisticsPanel && (this.order.status === 2 || this.order.status === 3)) {
                            await this.loadLogistics();
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

            openShipModal() {
                this.shipment.companyName = '';
                this.shipment.trackingNo = '';
                this.showShipModal = true;
            },

            async submitShipment() {
                if (!this.shipment.companyName || !this.shipment.trackingNo) {
                    alert('请完整填写物流公司和运单号');
                    return;
                }

                try {
                    const response = await fetch(`/api/v1/admin/orders/${orderId}/shipments`, {
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

                    if (result.success) {
                        this.showShipModal = false;
                        this.loadOrderDetail();
                    } else {
                        alert(result.message || '发货失败');
                    }
                } catch (error) {
                    console.error('发货异常:', error);
                    alert('发货失败，请稍后重试');
                }
            },

            async adminCancelOrder(orderId) {
                if (!confirm('确定要强制取消这个订单吗？此操作不可恢复！')) return;

                try {
                    const response = await fetch(`/api/v1/admin/orders/${orderId}/cancel`, {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'Accept': 'application/json'
                        },
                        body: JSON.stringify({ reason: '后台强制取消' })
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

            async showLogistics() {
                this.showLogisticsPanel = !this.showLogisticsPanel;
                if (this.showLogisticsPanel) await this.loadLogistics();
            },

            async loadLogistics() {
                this.logisticsLoading = true;
                try {
                    const response = await fetch(`/api/v1/admin/orders/${orderId}/logistics`, { headers: { 'Accept': 'application/json' } });
                    const result = await response.json();
                    this.logistics = response.ok && result.success ? result.data : null;
                    if (this.logistics) this.trackForm.status = Math.max(1, this.logistics.status);
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

            async addTrack() {
                if (!this.logistics || !this.trackForm.trackDesc) { alert('请填写轨迹描述'); return; }
                const response = await fetch(`/api/v1/admin/logistics/${this.logistics.logisticsId}/tracks`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
                    body: JSON.stringify({ trackDesc: this.trackForm.trackDesc, trackTime: new Date().toISOString(), location: this.trackForm.location || null, status: this.trackForm.status })
                });
                const result = await response.json();
                if (!response.ok || !result.success) { alert(result.message || '轨迹添加失败'); return; }
                this.trackForm.trackDesc = ''; this.trackForm.location = ''; await this.loadLogistics();
            },

            shipOrder(orderId) {
                this.openShipModal();
            }
        }
    }).mount('#adminOrderDetailApp');
})();
