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
                    // 直接调用预警接口（后端返回已过滤的低库存数据）
                    const resp = await fetch(`/api/v1/admin/inventory/warnings?page=${page}&pageSize=${pagination.value.pageSize}`, {
                        headers: { 'Accept': 'application/json' }
                    });
                    const data = await resp.json();

                    if (data.success && data.data && data.data.items) {
                        // 接口已返回预警数据，但需要补商品名称
                        const productMap = {};
                        let pPage = 1;
                        while (true) {
                            const pResp = await fetch(`/api/v1/admin/products?page=${pPage}&pageSize=100`, {
                                headers: { 'Accept': 'application/json' }
                            });
                            const pData = await pResp.json();
                            if (pData.success && pData.data && pData.data.items) {
                                for (const p of pData.data.items) productMap[p.productId] = p.name;
                                if (pPage >= pData.data.totalPages || pData.data.totalPages === 0) break;
                                pPage++;
                            } else break;
                        }

                        let list = data.data.items.map(it => ({
                            skuId: it.skuId,
                            productId: it.productId,
                            productName: productMap[it.productId] || '',
                            specDesc: formatSpec(it.specDescJson),
                            stock: it.stock,
                            warningStock: it.warningStock
                        }));

                        if (keyword.value) {
                            const kw = keyword.value.toLowerCase();
                            list = list.filter(r =>
                                (r.productName && r.productName.toLowerCase().includes(kw)) ||
                                (r.specDesc && r.specDesc.toLowerCase().includes(kw))
                            );
                        }

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
