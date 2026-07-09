(function () {
    const { createApp, ref, onMounted } = Vue;

    createApp({
        setup() {
            const loading = ref(false);
            const stats = ref({
                productCount: 0,
                onSaleCount: 0,
                offSaleCount: 0,
                categoryCount: 0,
                skuCount: 0,
                totalStock: 0,
                warningCount: 0
            });
            const systemStatus = ref({
                dbConnected: false,
                dbServer: '-',
                dbTime: '-'
            });
            const recentLogs = ref([]);

            function getLogTypeText(t) {
                switch (t) {
                    case 1: return '入库';
                    case 2: return '出库';
                    case 3: return '盘点';
                    default: return '其他';
                }
            }

            function getLogTypeClass(t) {
                switch (t) {
                    case 1: return 'text-bg-success';
                    case 2: return 'text-bg-danger';
                    case 3: return 'text-bg-info';
                    default: return 'text-bg-secondary';
                }
            }

            function formatTime(iso) {
                if (!iso) return '-';
                try {
                    return new Date(iso).toLocaleString('zh-CN', { hour12: false });
                } catch {
                    return iso;
                }
            }

            async function loadAll() {
                loading.value = true;
                try {
                    // 1. 系统健康检查
                    try {
                        const healthResp = await fetch('/api/health');
                        const health = await healthResp.json();
                        systemStatus.value.dbConnected = health.connected;
                        systemStatus.value.dbServer = health.database || 'Oracle';
                        systemStatus.value.dbTime = health.serverTime || '-';
                    } catch {
                        systemStatus.value.dbConnected = false;
                    }

                    // 2. 商品统计
                    const prodResp = await fetch('/api/v1/admin/products?page=1&pageSize=1');
                    const prodData = await prodResp.json();
                    if (prodData.success && prodData.data) {
                        stats.value.productCount = prodData.data.totalCount || 0;
                    }

                    // 3. 分类统计
                    const catResp = await fetch('/api/v1/admin/categories');
                    const catData = await catResp.json();
                    if (catData.success && catData.data) {
                        let count = 0;
                        function countAll(nodes) {
                            for (const n of nodes) {
                                count++;
                                if (n.children && n.children.length) countAll(n.children);
                            }
                        }
                        countAll(catData.data);
                        stats.value.categoryCount = count;
                    }

                    // 4. SKU 统计 + 上下架统计
                    const allProducts = [];
                    let pPage = 1;
                    while (true) {
                        const resp = await fetch(`/api/v1/admin/products?page=${pPage}&pageSize=100`);
                        const data = await resp.json();
                        if (data.success && data.data && data.data.items) {
                            for (const p of data.data.items) allProducts.push(p);
                            if (p.status === 1) stats.value.onSaleCount++;
                            else if (p.status === 0) stats.value.offSaleCount++;
                            if (pPage >= data.data.totalPages || data.data.totalPages === 0) break;
                            pPage++;
                        } else break;
                    }

                    let totalStock = 0;
                    for (const p of allProducts) {
                        const skuResp = await fetch(`/api/v1/admin/products/${p.productId}/skus`);
                        const skuData = await skuResp.json();
                        if (skuData.success && skuData.data) {
                            stats.value.skuCount += skuData.data.length;
                            for (const sku of skuData.data) {
                                totalStock += (sku.stock || 0);
                                if (sku.stock <= sku.warningStock) {
                                    stats.value.warningCount++;
                                }
                            }
                        }
                    }
                    stats.value.totalStock = totalStock;

                    // 5. 最近库存日志
                    const logResp = await fetch('/api/v1/admin/inventory/logs?page=1&pageSize=5');
                    const logData = await logResp.json();
                    if (logData.success && logData.data && logData.data.items) {
                        recentLogs.value = logData.data.items;
                    }
                } catch (err) {
                    console.error('加载Dashboard数据失败:', err);
                } finally {
                    loading.value = false;
                }
            }

            onMounted(() => {
                loadAll();
            });

            return {
                loading, stats, systemStatus, recentLogs,
                getLogTypeText, getLogTypeClass, formatTime,
                loadAll
            };
        }
    }).mount('#dashboardApp');
})();
