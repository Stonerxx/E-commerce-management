(function () {
    const { createApp, ref, computed, onMounted } = Vue;

    createApp({
        setup() {
            const loading = ref(false);
            const rows = ref([]);
            const keyword = ref('');
            const status = ref(null);
            const lowStock = ref(null);
            const errorMsg = ref('');

            const pagination = ref({
                currentPage: 1,
                pageSize: 10,
                total: 0,
                totalPages: 0
            });

            const adjustForm = ref({
                skuId: null,
                productName: '',
                specDesc: '',
                currentStock: 0,
                changeType: 1,
                changeQty: 0,
                remark: ''
            });
            const adjusting = ref(false);
            const adjustError = ref('');

            let adjustModal = null;

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

            async function loadSkus(page = 1) {
                const pageNum = typeof page === 'number' && !isNaN(page) && page > 0 ? page : 1;
                pagination.value.currentPage = pageNum;
                loading.value = true;
                errorMsg.value = '';
                try {
                    const hashMatch = /^#adjust-(\d+)$/.exec(window.location.hash);
                    const params = new URLSearchParams({
                        pageIndex: String(hashMatch ? 1 : pageNum),
                        pageSize: String(hashMatch ? 1 : pagination.value.pageSize)
                    });
                    if (hashMatch) params.set('skuId', hashMatch[1]);
                    if (keyword.value.trim()) params.set('keyword', keyword.value.trim());
                    if (status.value != null) params.set('status', String(status.value));
                    if (lowStock.value === 1) params.set('lowStock', 'true');

                    const response = await fetch(`/api/v1/admin/skus?${params.toString()}`, {
                        headers: { 'Accept': 'application/json' }
                    });
                    const payload = await response.json();
                    if (!response.ok || !payload.success || !payload.data) {
                        throw new Error(payload.message || `请求失败（${response.status}）`);
                    }

                    const data = payload.data;
                    rows.value = (data.items || []).map(sku => ({
                        ...sku,
                        price: Number(sku.price || 0),
                        stock: Number(sku.stock || 0),
                        lockedStock: Number(sku.lockedStock || 0),
                        warningStock: Number(sku.warningStock || 0),
                        specDesc: formatSpec(sku.specDescJson)
                    }));
                    pagination.value.currentPage = data.pageIndex || pageNum;
                    pagination.value.pageSize = data.pageSize || pagination.value.pageSize;
                    pagination.value.total = Number(data.totalCount || 0);
                    pagination.value.totalPages = Number(data.totalPages || 0);

                    if (hashMatch && rows.value.length === 1) {
                        openAdjustModal(rows.value[0]);
                        history.replaceState(null, '', window.location.pathname);
                    }
                } catch (err) {
                    console.error('加载SKU失败:', err);
                    rows.value = [];
                    pagination.value.total = 0;
                    pagination.value.totalPages = 0;
                    errorMsg.value = err.message || 'SKU 数据加载失败，请稍后重试';
                } finally {
                    loading.value = false;
                }
            }

            async function toggleStatus(row) {
                const newStatus = row.status === 1 ? 0 : 1;
                if (!confirm(`确定要${newStatus === 1 ? '上架' : '下架'}此 SKU 吗？`)) return;
                try {
                    const resp = await fetch(`/api/v1/admin/skus/${row.skuId}/status`, {
                        method: 'PUT',
                        headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
                        body: JSON.stringify({ status: newStatus })
                    });
                    const data = await resp.json();
                    if (data.success) {
                        row.status = newStatus;
                    } else {
                        alert(data.message || '操作失败');
                    }
                } catch (err) {
                    alert('网络错误：' + err.message);
                }
            }

            function resetFilters() {
                keyword.value = '';
                status.value = null;
                lowStock.value = null;
                loadSkus(1);
            }

            function openAdjustModal(row) {
                adjustForm.value = {
                    skuId: row.skuId,
                    productName: row.productName,
                    specDesc: row.specDesc,
                    currentStock: row.stock,
                    changeType: 1,
                    changeQty: 0,
                    remark: ''
                };
                adjustError.value = '';
                if (!adjustModal) adjustModal = new bootstrap.Modal(document.getElementById('adjustModal'));
                adjustModal.show();
            }

            async function submitAdjust() {
                adjustError.value = '';
                const quantity = Number(adjustForm.value.changeQty);
                if (!Number.isFinite(quantity) || quantity < 0 || (adjustForm.value.changeType !== 3 && quantity === 0)) {
                    adjustError.value = '请输入有效的数量';
                    return;
                }
                if (adjustForm.value.changeType === 2 && quantity > adjustForm.value.currentStock) {
                    adjustError.value = '出库数量不能超过当前库存';
                    return;
                }
                adjusting.value = true;
                try {
                    let qty = quantity;
                    if (adjustForm.value.changeType === 2) qty = -qty; // 出库传负数
                    if (adjustForm.value.changeType === 3) qty = quantity - adjustForm.value.currentStock;
                    const resp = await fetch(`/api/v1/admin/skus/${adjustForm.value.skuId}/inventory-adjustments`, {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json', 'Accept': 'application/json' },
                        body: JSON.stringify({
                            changeQty: qty,
                            remark: adjustForm.value.remark || ''
                        })
                    });
                    const data = await resp.json();
                    if (data.success) {
                        adjustModal.hide();
                        await loadSkus(pagination.value.currentPage);
                    } else {
                        adjustError.value = data.message || '调整失败';
                    }
                } catch (err) {
                    adjustError.value = '网络错误：' + err.message;
                } finally {
                    adjusting.value = false;
                }
            }

            onMounted(() => {
                loadSkus();
            });

            return {
                loading, rows, keyword, status, lowStock, errorMsg, pagination, pageNumbers,
                adjustForm, adjusting, adjustError,
                loadSkus, resetFilters, toggleStatus, openAdjustModal, submitAdjust
            };
        }
    }).mount('#skusApp');
})();
