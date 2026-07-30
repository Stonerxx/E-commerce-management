(function () {
    const { createApp } = Vue;

    // 从 URL 获取 cartItemIds（由购物车跳转带入）
    const cartItemIds = window.__CART_ITEM_IDS || '';
    const idList = cartItemIds ? cartItemIds.split(',').map(id => parseInt(id)).filter(id => id > 0) : [];

    createApp({
        data() {
            return {
                loading: false,
                submitting: false,
                addresses: [],
                coupons: [],
                couponCandidates: [],
                selectedAddressId: null,
                selectedCouponId: null,
                previewData: null,
                errorMessage: '',
                couponMessage: '',
                remark: ''
            };
        },
        mounted() {
            if (idList.length === 0) {
                alert('请从购物车选择商品后进入结算');
                window.location.href = '/cart';
                return;
            }
            this.initPage();
        },
        methods: {
            async initPage() {
                this.loading = true;
                this.errorMessage = '';
                this.previewData = null;
                this.selectedAddressId = null;
                try {
                    await this.loadAddresses();
                    await this.loadCoupons();

                    const defaultAddr = this.addresses.find(a => a.isDefault);
                    if (defaultAddr) {
                        this.selectedAddressId = defaultAddr.addressId;
                    } else if (this.addresses.length > 0) {
                        this.selectedAddressId = this.addresses[0].addressId;
                    }

                    if (this.selectedAddressId) {
                        await this.loadPreview();
                        if (this.previewData) {
                            await this.loadAvailableCoupons();
                        }
                    }
                } catch (error) {
                    console.error('初始化页面失败:', error);
                    this.errorMessage = error instanceof Error ? error.message : '确认订单页加载失败';
                } finally {
                    this.loading = false;
                }
            },

            async loadAddresses() {
                const response = await fetch('/api/v1/addresses', {
                    headers: { 'Accept': 'application/json' }
                });
                const result = await response.json().catch(() => null);
                if (!response.ok || !result?.success) {
                    throw new Error(result?.message || '收货地址加载失败');
                }
                this.addresses = Array.isArray(result.data) ? result.data : [];
            },

            async loadCoupons() {
                this.couponMessage = '';
                try {
                    const response = await fetch('/api/v1/coupons', {
                        headers: { 'Accept': 'application/json' }
                    });
                    const result = await response.json().catch(() => null);
                    if (!response.ok || !result?.success) {
                        throw new Error(result?.message || '优惠券加载失败');
                    }
                    this.couponCandidates = Array.isArray(result.data)
                        ? result.data.filter(coupon => coupon.status === 0)
                        : [];
                    this.coupons = [];
                } catch (error) {
                    console.error('加载优惠券异常:', error);
                    this.couponCandidates = [];
                    this.coupons = [];
                    this.couponMessage = error instanceof Error ? error.message : '优惠券加载失败';
                }
            },

            async loadAvailableCoupons() {
                if (this.couponCandidates.length === 0) {
                    this.coupons = [];
                    return;
                }

                try {
                    const validated = await Promise.all(this.couponCandidates.map(async coupon => {
                        const response = await fetch(`/api/v1/coupons/${coupon.userCouponId}/validate`, {
                            method: 'POST',
                            headers: {
                                'Content-Type': 'application/json',
                                'Accept': 'application/json'
                            },
                            body: JSON.stringify({ cartItemIds: idList })
                        });
                        const result = await response.json().catch(() => null);
                        if (!response.ok || !result?.success || !result.data) {
                            throw new Error(result?.message || '优惠券校验失败');
                        }
                        return result.data.available
                            ? {
                                ...coupon,
                                discountAmount: Number(result.data.discountAmount || 0),
                                eligibleAmount: Number(result.data.eligibleAmount || 0),
                                applicableCategoryName: result.data.applicableCategoryName || coupon.applicableCategoryName
                            }
                            : null;
                    }));

                    this.coupons = validated
                        .filter(Boolean)
                        .sort((left, right) => right.discountAmount - left.discountAmount);
                    if (!this.coupons.some(coupon => coupon.userCouponId === this.selectedCouponId)) {
                        this.selectedCouponId = null;
                    }
                } catch (error) {
                    console.error('校验优惠券异常:', error);
                    this.coupons = [];
                    this.selectedCouponId = null;
                    this.couponMessage = error instanceof Error ? error.message : '优惠券校验失败';
                }
            },

            async loadPreview() {
                if (!this.selectedAddressId) {
                    this.previewData = null;
                    return;
                }

                this.errorMessage = '';
                try {
                    const requestBody = {
                        addressId: this.selectedAddressId,
                        userCouponId: this.selectedCouponId,
                        cartItemIds: idList,
                        remark: this.remark
                    };

                    const response = await fetch('/api/v1/orders/preview', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'Accept': 'application/json'
                        },
                        body: JSON.stringify(requestBody)
                    });
                    const result = await response.json().catch(() => null);

                    if (response.ok && result?.success && result.data) {
                        this.previewData = result.data;
                    } else {
                        throw new Error(result?.message || '订单预览加载失败');
                    }
                } catch (error) {
                    console.error('加载预览异常:', error);
                    this.previewData = null;
                    this.errorMessage = error instanceof Error ? error.message : '订单预览加载失败';
                }
            },

            async submitOrder() {
                if (!this.selectedAddressId) {
                    alert('请选择收货地址');
                    return;
                }

                if (idList.length === 0) {
                    alert('购物车为空，请先添加商品');
                    return;
                }

                this.submitting = true;
                try {
                    const requestBody = {
                        addressId: this.selectedAddressId,
                        userCouponId: this.selectedCouponId,
                        cartItemIds: idList,
                        remark: this.remark
                    };

                    const response = await fetch('/api/v1/orders', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'Accept': 'application/json'
                        },
                        body: JSON.stringify(requestBody)
                    });
                    const result = await response.json();

                    if (result.success && result.data) {
                        const orderId = result.data.orderId;
                        alert('订单创建成功！');
                        window.location.href = `/payment/${orderId}`;
                    } else {
                        alert(result.message || '创建订单失败，请重试');
                    }
                } catch (error) {
                    console.error('提交订单异常:', error);
                    alert('提交订单失败，请检查网络连接');
                } finally {
                    this.submitting = false;
                }
            },

            formatMoney(value) {
                return Number(value || 0).toFixed(2);
            }
        },
        watch: {
            selectedAddressId() {
                // 地址变化时重新计算运费或校验，这里重新加载预览
                if (!this.loading) {
                    this.loadPreview();
                }
            },
            selectedCouponId() {
                if (!this.loading && this.selectedAddressId) {
                    this.loadPreview();
                }
            },
            remark() {
                // 备注变化不影响金额，但如果需要可以在这里防抖更新预览
            }
        }
    }).mount('#checkoutApp');
})();
