(function () {
    const { createApp, ref, computed, onMounted } = Vue;

    createApp({
        setup() {
            const loading = ref(false);
            const rows = ref([]);
            const keyword = ref('');
            const status = ref(null);
            const lowStock = ref(null);

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
                try {
                    // 1. 加载所有商品用于展示名称
                    const productList = [];
                    let productPage = 1;
                    while (true) {
                        const resp = await fetch(`/api/v1/admin/products?pageIndex=${productPage}&pageSize=100`, {
                            headers: { 'Accept': 'application/json' }
                        });
                        const data = await resp.json();
                        if (data.success && data.data && data.data.items) {
                            for (const p of data.data.items) productList.push(p);
                            if (productPage >= data.data.totalPages || data.data.totalPages === 0) break;
                            productPage++;
                        } else break;
                    }

                    // 2. 加载每个商品的 SKU
                    const allSkus = [];
                    for (const product of productList) {
                        const skuResp = await fetch(`/api/v1/admin/products/${product.productId}/skus`, {
                            headers: { 'Accept': 'application/json' }
                        });
                        const skuData = await skuResp.json();
                        if (skuData.success && skuData.data) {
                            for (const sku of skuData.data) {
                                allSkus.push({
                                    skuId: sku.skuId,
                                    productId: sku.productId,
                                    productName: product.name,
                                    specDesc: formatSpec(sku.specDescJson),
                                    specDescJson: sku.specDescJson,
                                    price: sku.price,
                                    stock: sku.stock,
                                    lockedStock: sku.lockedStock || 0,
                                    warningStock: sku.warningStock || 0,
                                    status: sku.status
                                });
                            }
                        }
                    }

                    // 3. 过滤
                    let filtered = allSkus;
                    if (keyword.value) {
                        const kw = keyword.value.toLowerCase();
                        filtered = filtered.filter(r =>
                            (r.productName && r.productName.toLowerCase().includes(kw)) ||
                            (r.specDesc && r.specDesc.toLowerCase().includes(kw))
                        );
                    }
                    if (status.value != null) filtered = filtered.filter(r => r.status === status.value);
                    if (lowStock.value === 1) filtered = filtered.filter(
                        r => r.stock - r.lockedStock <= r.warningStock);

                    // 4. 分页（搜索或筛选后重置到第1页）
                    const total = filtered.length;
                    const totalPages = Math.max(1, Math.ceil(total / pagination.value.pageSize));
                    const targetPage = Math.min(pageNum, totalPages);
                    const start = (targetPage - 1) * pagination.value.pageSize;
                    rows.value = filtered.slice(start, start + pagination.value.pageSize);
                    pagination.value.total = total;
                    pagination.value.totalPages = totalPages;
                    pagination.value.currentPage = targetPage;

                    const hashMatch = /^#adjust-(\d+)$/.exec(window.location.hash);
                    if (hashMatch) {
                        const targetSkuId = Number(hashMatch[1]);
                        const targetSku = allSkus.find(sku => sku.skuId === targetSkuId);
                        if (targetSku) {
                            openAdjustModal(targetSku);
                            history.replaceState(null, '', window.location.pathname);
                        }
                    }
                } catch (err) {
                    console.error('加载SKU失败:', err);
                    alert('加载SKU失败：' + err.message);
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
                if (!adjustForm.value.changeQty || adjustForm.value.changeQty < 0) {
                    adjustError.value = '请输入有效的数量';
                    return;
                }
                if (adjustForm.value.changeType === 2 && adjustForm.value.changeQty > adjustForm.value.currentStock) {
                    adjustError.value = '出库数量不能超过当前库存';
                    return;
                }
                adjusting.value = true;
                try {
                    let qty = adjustForm.value.changeQty;
                    if (adjustForm.value.changeType === 2) qty = -qty; // 出库传负数
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
                loading, rows, keyword, status, lowStock, pagination, pageNumbers,
                adjustForm, adjusting, adjustError,
                loadSkus, toggleStatus, openAdjustModal, submitAdjust
            };
        }
    }).mount('#skusApp');
})();
