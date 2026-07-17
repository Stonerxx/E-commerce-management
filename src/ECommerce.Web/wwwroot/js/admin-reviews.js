(function () {
    const { createApp } = Vue;
    createApp({
        data() { return { loading: false, items: [], pageIndex: 1, pageSize: 10, totalCount: 0, totalPages: 1, filters: { productId: '', status: '' } }; },
        mounted() { this.load(); },
        methods: {
            async load() {
                this.loading = true;
                const params = new URLSearchParams({ pageIndex: this.pageIndex, pageSize: this.pageSize });
                if (this.filters.productId) params.set('productId', this.filters.productId);
                if (this.filters.status !== '') params.set('status', this.filters.status);
                try {
                    const response = await fetch(`/api/v1/admin/reviews?${params}`, { headers: { Accept: 'application/json' } });
                    const result = await response.json();
                    if (!response.ok || !result.success) throw new Error(result.message || '加载失败');
                    this.items = result.data.items || []; this.totalCount = result.data.totalCount || 0; this.totalPages = Math.max(1, result.data.totalPages || 1);
                } catch (error) { alert(error.message || '加载失败'); } finally { this.loading = false; }
            },
            search() { this.pageIndex = 1; this.load(); },
            goPage(page) { if (page < 1 || page > this.totalPages) return; this.pageIndex = page; this.load(); },
            async setStatus(item, status) {
                const response = await fetch(`/api/v1/admin/reviews/${item.reviewId}/status`, { method: 'PUT', headers: { 'Content-Type': 'application/json', Accept: 'application/json' }, body: JSON.stringify({ status }) });
                const result = await response.json();
                if (!response.ok || !result.success) { alert(result.message || '审核失败'); return; }
                await this.load();
            },
            statusText(status) { return ({ 0: '待审核', 1: '已发布', 2: '已屏蔽' })[status] || '未知'; },
            statusClass(status) { return ({ 0: 'text-bg-warning', 1: 'text-bg-success', 2: 'text-bg-secondary' })[status] || 'text-bg-secondary'; },
            formatDate(value) { return value ? new Date(value).toLocaleString('zh-CN') : '-'; }
        }
    }).mount('#adminReviewsApp');
})();
