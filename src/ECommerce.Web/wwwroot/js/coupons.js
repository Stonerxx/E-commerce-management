(function () {
    const { createApp } = Vue;

    createApp({
        data() {
            return { tab: 'available', loading: true, available: [], mine: [], receivingId: null };
        },
        mounted() {
            this.reload();
        },
        methods: {
            async reload() {
                this.loading = true;
                try {
                    const [availableResponse, mineResponse] = await Promise.all([
                        fetch('/api/v1/coupon-templates/available', { headers: { Accept: 'application/json' } }),
                        fetch('/api/v1/coupons', { headers: { Accept: 'application/json' } })
                    ]);
                    const [availableResult, mineResult] = await Promise.all([availableResponse.json(), mineResponse.json()]);
                    if (!availableResult.success) throw new Error(availableResult.message);
                    if (!mineResult.success) throw new Error(mineResult.message);
                    this.available = availableResult.data || [];
                    this.mine = mineResult.data || [];
                } catch (error) {
                    console.error('加载优惠券失败:', error);
                    alert(error.message || '加载优惠券失败');
                } finally {
                    this.loading = false;
                }
            },
            async receive(item) {
                this.receivingId = item.templateId;
                try {
                    const response = await fetch(`/api/v1/coupon-templates/${item.templateId}/receive`, {
                        method: 'POST', headers: { Accept: 'application/json' }
                    });
                    const result = await response.json();
                    if (!response.ok || !result.success) throw new Error(result.message || '领取失败');
                    await this.reload();
                    this.tab = 'mine';
                } catch (error) {
                    alert(error.message || '领取失败');
                } finally {
                    this.receivingId = null;
                }
            },
            discountText(item) {
                return item.type === 1 ? `¥${item.amount.toFixed(2)}` : `${(item.amount * 10).toFixed(1)} 折`;
            },
            remainingText(item) {
                return item.totalCount === -1 ? '不限量' : Math.max(0, item.totalCount - item.receivedCount);
            },
            statusText(status) {
                return ({ 0: '未使用', 1: '已使用', 2: '已失效' })[status] || '未知';
            },
            statusClass(status) {
                return ({ 0: 'text-bg-success', 1: 'text-bg-secondary', 2: 'text-bg-dark' })[status] || 'text-bg-secondary';
            },
            formatDate(value) { return value ? new Date(value).toLocaleDateString('zh-CN') : '-'; },
            formatDateTime(value) { return value ? new Date(value).toLocaleString('zh-CN') : '-'; }
        }
    }).mount('#couponsApp');
})();
