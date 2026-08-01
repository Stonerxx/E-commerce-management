(function () {
    const { createApp, ref, computed, onMounted } = Vue;

    createApp({
        setup() {
            const loading = ref(true);
            const errorMsg = ref('');
            const addingToCart = ref(false);
            const product = ref(null);
            const selectedImage = ref('');
            const quantity = ref(1);
            const selectedSpecs = ref({});
            const reviews = ref([]);
            const reviewsLoading = ref(false);
            const reviewPage = ref(1);
            const reviewTotalCount = ref(0);
            const reviewTotalPages = ref(1);
            const recommendations = ref([]);
            const recommendationsLoading = ref(false);

            const productId = parseInt(window.location.pathname.split('/')[2]);

            function parseSkuSpecs(sku) {
                try {
                    const parsed = JSON.parse(sku.specDescJson);
                    return parsed && typeof parsed === 'object' && !Array.isArray(parsed)
                        ? parsed
                        : null;
                } catch {
                    return null;
                }
            }

            const enabledSkuSelections = computed(() => {
                if (!product.value || !Array.isArray(product.value.skus)) return [];
                return product.value.skus
                    .filter(sku => Number(sku.status) === 1)
                    .map(sku => ({ sku, specs: parseSkuSpecs(sku) }))
                    .filter(item => item.specs);
            });

            const specGroups = computed(() => {
                if (!product.value) return [];

                // SKU 中的 JSON 才是实际下单组合。规格定义用于排序和文案，但旧数据若漏了
                // 维度或选项，仍应以可售 SKU 为准，避免出现“选完了却不能购买”。
                const skuGroups = new Map();
                for (const item of enabledSkuSelections.value) {
                    for (const [name, value] of Object.entries(item.specs)) {
                        if (!skuGroups.has(name)) {
                            skuGroups.set(name, []);
                        }
                        if (!skuGroups.get(name).includes(value)) {
                            skuGroups.get(name).push(value);
                        }
                    }
                }

                if (skuGroups.size === 0) return [];

                const definedOrder = [];
                const definedValues = new Map();
                for (const spec of (product.value.specs || [])) {
                    if (!skuGroups.has(spec.specName)) continue;
                    if (!definedValues.has(spec.specName)) {
                        definedOrder.push(spec.specName);
                        definedValues.set(spec.specName, []);
                    }
                    if (!definedValues.get(spec.specName).includes(spec.specValue)) {
                        definedValues.get(spec.specName).push(spec.specValue);
                    }
                }

                const names = [
                    ...definedOrder,
                    ...Array.from(skuGroups.keys()).filter(name => !definedValues.has(name))
                ];

                return names.map(name => {
                    const skuValues = skuGroups.get(name);
                    const preferredValues = (definedValues.get(name) || [])
                        .filter(value => skuValues.includes(value));
                    return {
                        specName: name,
                        values: [
                            ...preferredValues,
                            ...skuValues.filter(value => !preferredValues.includes(value))
                        ]
                    };
                });
            });

            const missingSpecNames = computed(() => specGroups.value
                .map(group => group.specName)
                .filter(name => !selectedSpecs.value[name]));

            const currentSku = computed(() => {
                if (enabledSkuSelections.value.length === 0) return null;

                const requiredNames = specGroups.value.map(group => group.specName);
                if (requiredNames.length === 0) {
                    return enabledSkuSelections.value.length === 1
                        ? enabledSkuSelections.value[0].sku
                        : null;
                }
                if (missingSpecNames.value.length > 0) return null;

                const match = enabledSkuSelections.value.find(item =>
                    requiredNames.every(name => item.specs[name] === selectedSpecs.value[name]));
                return match ? match.sku : null;
            });

            const availableStock = computed(() => {
                if (!currentSku.value) return 0;
                return Math.max(0, Number(currentSku.value.stock || 0) - Number(currentSku.value.lockedStock || 0));
            });

            const currentSkuSpecText = computed(() => {
                if (!currentSku.value) return '';
                return specGroups.value
                    .map(group => `${group.specName}: ${selectedSpecs.value[group.specName]}`)
                    .join(' / ');
            });

            const canPurchase = computed(() => Boolean(currentSku.value) && availableStock.value > 0);

            const selectionHint = computed(() => {
                if (enabledSkuSelections.value.length === 0) return '暂无可购买规格';
                if (missingSpecNames.value.length > 0) {
                    return `请选择：${missingSpecNames.value.join('、')}`;
                }
                if (!currentSku.value) return '该规格组合暂不可购买，请重新选择';
                if (availableStock.value <= 0) return '当前规格库存不足';
                return '';
            });

            function isSpecOptionAvailable(specName, specValue) {
                const groupIndex = specGroups.value.findIndex(group => group.specName === specName);
                if (groupIndex < 0) return false;

                const candidate = { [specName]: specValue };
                for (let index = 0; index < groupIndex; index++) {
                    const previousName = specGroups.value[index].specName;
                    const previousValue = selectedSpecs.value[previousName];
                    if (previousValue) candidate[previousName] = previousValue;
                }

                return enabledSkuSelections.value.some(item =>
                    Object.entries(candidate).every(([name, value]) => item.specs[name] === value));
            }

            function selectSpec(specName, specValue) {
                if (!isSpecOptionAvailable(specName, specValue)) return;

                const groupIndex = specGroups.value.findIndex(group => group.specName === specName);
                const nextSelection = { ...selectedSpecs.value, [specName]: specValue };
                const hasMatchingSku = selection => enabledSkuSelections.value.some(item =>
                    Object.entries(selection).every(([name, value]) => item.specs[name] === value));

                // 更改前置维度时，仅清理与新选择冲突的后续维度；兼容组合保留。
                if (!hasMatchingSku(nextSelection)) {
                    for (let index = specGroups.value.length - 1; index > groupIndex; index--) {
                        delete nextSelection[specGroups.value[index].specName];
                        if (hasMatchingSku(nextSelection)) break;
                    }
                }
                selectedSpecs.value = nextSelection;
                quantity.value = 1;
            }

            function normalizeQuantity() {
                const normalized = Math.floor(Number(quantity.value));
                quantity.value = Number.isFinite(normalized) ? Math.max(1, normalized) : 1;
                if (availableStock.value > 0) {
                    quantity.value = Math.min(quantity.value, availableStock.value);
                }
            }

            async function loadProduct() {
                loading.value = true;
                errorMsg.value = '';
                try {
                    const response = await fetch(`/api/v1/products/${productId}`, {
                        headers: { 'Accept': 'application/json' }
                    });
                    const result = await response.json();

                    if (result.success && result.data) {
                        product.value = result.data;
                        selectedImage.value = result.data.mainImage;
                        selectedSpecs.value = {};
                    } else {
                        errorMsg.value = result.message || '商品不存在';
                    }
                } catch (error) {
                    errorMsg.value = '加载商品失败，请稍后重试';
                    console.error('加载商品失败:', error);
                } finally {
                    loading.value = false;
                }
            }

            async function addToCart() {
                if (!currentSku.value) return;

                normalizeQuantity();
                if (availableStock.value <= 0 || quantity.value > availableStock.value) {
                    window.appToast?.('当前库存不足，请调整购买数量', 'warning');
                    return;
                }

                addingToCart.value = true;
                try {
                    const response = await fetch('/api/v1/cart/items', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'Accept': 'application/json'
                        },
                        body: JSON.stringify({
                            skuId: currentSku.value.skuId,
                            quantity: quantity.value
                        })
                    });

                    if (response.status === 401) {
                        window.location.href = '/account/login';
                        return;
                    }
                    if (response.status === 403) {
                        window.appToast?.('当前账号没有加入购物车权限', 'warning');
                        return;
                    }

                    const result = await response.json().catch(() => null);

                    if (response.ok && result?.success) {
                        window.appToast?.('已添加到购物车', 'success');
                    } else {
                        window.appToast?.(result?.message || '添加失败', 'danger');
                    }
                } catch (error) {
                    console.error('加入购物车失败:', error);
                    window.appToast?.('添加失败，请稍后重试', 'danger');
                } finally {
                    addingToCart.value = false;
                }
            }

            async function loadReviews(page = 1) {
                if (page < 1) return;
                reviewsLoading.value = true;
                try {
                    const response = await fetch(`/api/v1/products/${productId}/reviews?pageIndex=${page}&pageSize=5`, {
                        headers: { 'Accept': 'application/json' }
                    });
                    const result = await response.json();
                    if (!response.ok || !result.success) {
                        throw new Error(result.message || '评价加载失败');
                    }
                    reviews.value = result.data.items || [];
                    reviewPage.value = result.data.pageIndex || page;
                    reviewTotalCount.value = result.data.totalCount || 0;
                    reviewTotalPages.value = Math.max(1, result.data.totalPages || 1);
                } catch (error) {
                    console.error('加载评价失败:', error);
                    reviews.value = [];
                } finally {
                    reviewsLoading.value = false;
                }
            }

            async function loadRecommendations() {
                recommendationsLoading.value = true;
                try {
                    const response = await fetch(`/api/v1/products/${productId}/recommendations?limit=6`, {
                        headers: { 'Accept': 'application/json' }
                    });
                    const result = await response.json();
                    recommendations.value = response.ok && result.success ? (result.data || []) : [];
                } catch (error) {
                    console.error('加载推荐商品失败:', error);
                    recommendations.value = [];
                } finally {
                    recommendationsLoading.value = false;
                }
            }

            function formatReviewDate(value) {
                return value ? new Date(value).toLocaleString('zh-CN') : '-';
            }

            async function buyNow() {
                if (!currentSku.value) return;

                normalizeQuantity();
                if (availableStock.value <= 0 || quantity.value > availableStock.value) {
                    window.appToast?.('当前库存不足，请调整购买数量', 'warning');
                    return;
                }

                try {
                    const addressResponse = await fetch('/api/v1/addresses', {
                        headers: { 'Accept': 'application/json' }
                    });

                    if (addressResponse.status === 401) {
                        window.location.href = '/account/login';
                        return;
                    }

                    const addressResult = await addressResponse.json().catch(() => null);
                    if (!addressResponse.ok || !addressResult?.success) {
                        window.appToast?.(addressResult?.message || '收货地址检查失败', 'danger');
                        return;
                    }

                    if (!Array.isArray(addressResult.data) || addressResult.data.length === 0) {
                        window.appToast?.('请先添加收货地址后再购买', 'warning');
                        return;
                    }

                    const addResponse = await fetch('/api/v1/cart/items', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'Accept': 'application/json'
                        },
                        body: JSON.stringify({
                            skuId: currentSku.value.skuId,
                            quantity: quantity.value
                        })
                    });

                    if (addResponse.status === 401) {
                        window.location.href = '/account/login';
                        return;
                    }

                    if (addResponse.status === 403) {
                        window.appToast?.('当前账号没有立即购买权限', 'warning');
                        return;
                    }

                    const addResult = await addResponse.json().catch(() => null);
                    if (!addResponse.ok || !addResult?.success) {
                        window.appToast?.(addResult?.message || '操作失败', 'danger');
                        return;
                    }

                    const cartResponse = await fetch('/api/v1/cart', {
                        headers: { 'Accept': 'application/json' }
                    });
                    const cartResult = await cartResponse.json();
                    if (cartResult.success && cartResult.data && cartResult.data.items) {
                        const items = cartResult.data.items;
                        const targetItem = items.find(item =>
                            item.skuId === currentSku.value.skuId
                        );
                        if (targetItem) {
                            window.location.href = `/orders/create?cartItemIds=${targetItem.cartItemId}`;
                            return;
                        }
                    }
                    window.appToast?.('商品已加入购物车，请在购物车中结算', 'info');
                    window.location.href = '/cart';
                } catch (error) {
                    console.error('立即购买失败:', error);
                    window.appToast?.('操作失败，请稍后重试', 'danger');
                }
            }

            onMounted(() => {
                loadProduct();
                loadReviews();
                loadRecommendations();
            });

            return {
                loading,
                errorMsg,
                addingToCart,
                product,
                selectedImage,
                quantity,
                selectedSpecs,
                reviews,
                reviewsLoading,
                reviewPage,
                reviewTotalCount,
                reviewTotalPages,
                recommendations,
                recommendationsLoading,
                specGroups,
                currentSku,
                availableStock,
                currentSkuSpecText,
                canPurchase,
                selectionHint,
                isSpecOptionAvailable,
                selectSpec,
                normalizeQuantity,
                loadReviews,
                formatReviewDate,
                addToCart,
                buyNow
            };
        }
    }).mount('#productDetailApp');
})();
