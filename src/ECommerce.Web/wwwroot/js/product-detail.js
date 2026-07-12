(function () {
    try {
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
                const isCustomer = ref(false);

                let productId = null;

                const specGroups = computed(() => {
                    if (!product.value || !product.value.specs) return [];
                    const groups = {};
                    for (const spec of product.value.specs) {
                        if (!groups[spec.specName]) {
                            groups[spec.specName] = [];
                        }
                        if (!groups[spec.specName].includes(spec.specValue)) {
                            groups[spec.specName].push(spec.specValue);
                        }
                    }
                    return Object.entries(groups).map(([name, values]) => ({
                        specName: name,
                        values: values
                    }));
                });

                const currentSku = computed(() => {
                    if (!product.value || !product.value.skus) return null;
                    const enabledSkus = product.value.skus.filter(sku => sku.status === 1);
                    const selectedEntries = Object.entries(selectedSpecs.value)
                        .filter(([_, v]) => v);
                    if (selectedEntries.length === 0) return enabledSkus[0] || null;

                    for (const sku of enabledSkus) {
                        let specObj = {};
                        try {
                            specObj = JSON.parse(sku.specDescJson);
                        } catch {
                            continue;
                        }
                        let match = true;
                        for (const [name, val] of selectedEntries) {
                            if (specObj[name] !== val) {
                                match = false;
                                break;
                            }
                        }
                        if (match) return sku;
                    }
                    return null;
                });

                const availableStock = computed(() => {
                    if (!currentSku.value) return 0;
                    return Math.max(0, Number(currentSku.value.stock || 0) - Number(currentSku.value.lockedStock || 0));
                });

                const currentSkuSpecText = computed(() => {
                    if (!currentSku.value) return '';
                    try {
                        const specObj = JSON.parse(currentSku.value.specDescJson);
                        return Object.entries(specObj)
                            .map(([k, v]) => `${k}: ${v}`)
                            .join(' / ');
                    } catch {
                        return '';
                    }
                });

                function selectSpec(specName, specValue) {
                    selectedSpecs.value[specName] = specValue;
                }

                async function loadProduct() {
                    loading.value = true;
                    errorMsg.value = '';
                    try {
                        if (!productId || isNaN(productId)) {
                            errorMsg.value = '无效的商品ID';
                            loading.value = false;
                            return;
                        }

                        const response = await fetch(`/api/v1/products/${productId}`, {
                            headers: { 'Accept': 'application/json' }
                        });

                        if (!response.ok) {
                            throw new Error(`HTTP error! status: ${response.status}`);
                        }

                        const result = await response.json();

                        if (result.success && result.data) {
                            product.value = result.data;
                            selectedImage.value = result.data.mainImage;

                            if (result.data.skus && result.data.skus.length > 0) {
                                try {
                                    const firstSkuSpecs = JSON.parse(result.data.skus[0].specDescJson);
                                    selectedSpecs.value = { ...firstSkuSpecs };
                                } catch {
                                    selectedSpecs.value = {};
                                }
                            }
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
                            alert('当前账号没有加入购物车权限');
                            return;
                        }

                        const result = await response.json().catch(() => null);

                        if (response.ok && result?.success) {
                            alert('已添加到购物车');
                        } else {
                            alert(result?.message || '添加失败');
                        }
                    } catch (error) {
                        console.error('加入购物车失败:', error);
                        alert('添加失败，请稍后重试');
                    } finally {
                        addingToCart.value = false;
                    }
                }

                async function buyNow() {
                    if (!currentSku.value) return;

                    try {
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
                            alert('当前账号没有立即购买权限');
                            return;
                        }

                        const addResult = await addResponse.json().catch(() => null);
                        if (!addResponse.ok || !addResult?.success) {
                            alert(addResult?.message || '操作失败');
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
                        alert('请前往购物车结算');
                        window.location.href = '/cart';
                    } catch (error) {
                        console.error('立即购买失败:', error);
                        alert('操作失败，请稍后重试');
                    }
                }

                onMounted(() => {
                    const appEl = document.getElementById('productDetailApp');
                    if (appEl) {
                        const isCustomerAttr = appEl.getAttribute('data-is-customer');
                        isCustomer.value = isCustomerAttr === 'True';

                        const productIdAttr = appEl.getAttribute('data-product-id');
                        if (productIdAttr) {
                            productId = parseInt(productIdAttr);
                        }
                    }
                    loadProduct();
                });

                return {
                    loading,
                    errorMsg,
                    addingToCart,
                    product,
                    selectedImage,
                    quantity,
                    selectedSpecs,
                    isCustomer,
                    specGroups,
                    currentSku,
                    availableStock,
                    currentSkuSpecText,
                    selectSpec,
                    addToCart,
                    buyNow
                };
            }
        }).mount('#productDetailApp');
    } catch (error) {
        console.error('Vue application initialization failed:', error);
        const errorEl = document.getElementById('vueInitError');
        if (errorEl) {
            errorEl.style.display = 'block';
        }
        const appEl = document.getElementById('productDetailApp');
        if (appEl) {
            appEl.style.display = 'block';
        }
    }
})();
