(function () {
    const { createApp, ref, computed, onMounted } = Vue;

    createApp({
        setup() {
            const loading = ref(false);
            const rows = ref([]);
            const keyword = ref('');
            const changeType = ref(null);
            const skuId = ref('');

            const pagination = ref({
                currentPage: 1,
                pageSize: 15,
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

            function getTypeText(t) {
                switch (t) {
                    case 'ADJUST': return '人工调整';
                    case 'ORDER_LOCK': return '订单锁定';
                    case 'ORDER_RELEASE': return '订单释放';
                    case 'ORDER_DEDUCT': return '订单扣减';
                    case 'RESTOCK': return '入库';
                    case 'SALE': return '销售出库';
                    default: return t || '其他';
                }
            }

            function getTypeClass(t) {
                switch (t) {
                    case 'RESTOCK': return 'text-bg-success';
                    case 'SALE':
                    case 'ORDER_DEDUCT': return 'text-bg-danger';
                    case 'ORDER_LOCK': return 'text-bg-warning';
                    case 'ORDER_RELEASE': return 'text-bg-info';
                    default: return 'text-bg-secondary';
                }
            }

            function formatTime(iso) {
                if (!iso) return '-';
                try {
                    const d = new Date(iso);
                    return d.toLocaleString('zh-CN', { hour12: false });
                } catch {
                    return iso;
                }
            }

            async function loadLogs(page = 1) {
                pagination.value.currentPage = page;
                loading.value = true;
                try {
                    const params = new URLSearchParams();
                    params.set('pageIndex', page);
                    params.set('pageSize', pagination.value.pageSize);
                    if (changeType.value != null) params.set('changeType', changeType.value);
                    if (skuId.value) params.set('skuId', skuId.value);

                    const resp = await fetch(`/api/v1/admin/inventory/logs?${params.toString()}`, {
                        headers: { 'Accept': 'application/json' }
                    });
                    const data = await resp.json();

                    if (data.success && data.data) {
                        let list = (data.data.items || []).map(it => ({
                            logId: it.logId,
                            skuId: it.skuId,
                            productId: it.productId,
                            productName: it.productName || '',
                            changeType: it.changeType,
                            quantity: it.changeQty,
                            stockBefore: it.beforeStock,
                            stockAfter: it.afterStock,
                            operatorId: it.operatorId,
                            operatorName: it.operatorName,
                            createdAt: it.createdAt,
                            remark: it.remark
                        }));

                        if (keyword.value) {
                            const kw = keyword.value.toLowerCase();
                            list = list.filter(r =>
                                (r.productName && r.productName.toLowerCase().includes(kw)) ||
                                String(r.skuId).includes(kw)
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
                    console.error('加载日志失败:', err);
                    alert('加载日志失败：' + err.message);
                } finally {
                    loading.value = false;
                }
            }

            function resetFilters() {
                keyword.value = '';
                changeType.value = null;
                skuId.value = '';
                loadLogs();
            }

            function exportLogs() {
                const params = new URLSearchParams();
                if (changeType.value != null) params.set('changeType', changeType.value);
                if (skuId.value) params.set('skuId', skuId.value);
                window.location.assign(`/api/v1/admin/exports/inventory?${params.toString()}`);
            }

            onMounted(() => {
                loadLogs();
            });

            return {
                loading, rows, keyword, changeType, skuId,
                pagination, pageNumbers,
                getTypeText, getTypeClass, formatTime,
                loadLogs, resetFilters, exportLogs
            };
        }
    }).mount('#logsApp');
})();
