(function () {
    const { createApp, ref, computed, onMounted } = Vue;

    createApp({
        setup() {
            const loading = ref(false);
            const rows = ref([]);
            const keyword = ref('');

            const pagination = ref({
                currentPage: 1,
                pageSize: 10,
                total: 0,
                totalPages: 0
            });

            const pageNumbers = computed(() => {
                const pages = [];
                const total = pagination.value.totalPages;
                const current = pagination.value.currentPage;
                const start = Math.max(1, current - 2);
                const end = Math.min(total, current + 2);
                for (let i = start; i <= end; i++) pages.push(i);
                return pages;
            });

            function formatSpec(json) {
                if (!json) return '-';
                try {
                    const obj = JSON.parse(json);
                    return Object.entries(obj).map(([k, v]) => `${k}:${v}`).join(' / ');
                } catch {
                    return json;
                }
            }

            async function loadWarnings(page = 1) {
                pagination.value.currentPage = page;
                loading.value = true;
                try {
                    const params = new URLSearchParams({
                        pageIndex: page,
                        pageSize: pagination.value.pageSize
                    });
                    if (keyword.value.trim()) params.set('keyword', keyword.value.trim());
                    const resp = await fetch(`/api/v1/admin/inventory/warnings?${params}`, {
                        headers: { 'Accept': 'application/json' }
                    });
                    const data = await resp.json();

                    if (data.success && data.data && data.data.items) {
                        const list = data.data.items.map(it => ({
                            skuId: it.skuId,
                            productId: it.productId,
                            productName: it.productName || '',
                            specDesc: formatSpec(it.specDescJson),
                            stock: it.stock,
                            lockedStock: it.lockedStock,
                            availableStock: Math.max(0, it.stock - it.lockedStock),
                            warningStock: it.warningStock
                        }));

                        rows.value = list;
                        pagination.value.total = data.data.totalCount;
                        pagination.value.totalPages = data.data.totalPages;
                    } else {
                        rows.value = [];
                        pagination.value.total = 0;
                        pagination.value.totalPages = 0;
                    }
                } catch (err) {
                    console.error('加载预警失败:', err);
                    alert('加载预警失败：' + err.message);
                } finally {
                    loading.value = false;
                }
            }

            onMounted(() => {
                loadWarnings();
            });

            return {
                loading, rows, keyword, pagination, pageNumbers,
                loadWarnings
            };
        }
    }).mount('#warningsApp');
})();
