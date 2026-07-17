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
                selectedAddressId: null,
                selectedCouponId: null,
                previewData: null,
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
                    }
                } catch (error) {
                    console.error('初始化页面失败:', error);
                } finally {
                    this.loading = false;
                }
            },

            async loadAddresses() {
                try {
                    const response = await fetch('/api/v1/addresses', {
                        headers: { 'Accept': 'application/json' }
                    });
                    const result = await response.json();
                    if (result.success && result.data) {
                        this.addresses = result.data;
                    } else {
                        console.warn('加载地址失败:', result.message);
                    }
                } catch (error) {
                    console.error('加载地址异常:', error);
                }
            },

            async loadCoupons() {
                try {
                    const response = await fetch('/api/v1/coupons', {
                        headers: { 'Accept': 'application/json' }
                    });
                    const result = await response.json();
                    if (result.success && result.data) {
                        this.coupons = result.data.filter(coupon => coupon.status === 0);
                    } else {
                        console.warn('加载优惠券失败:', result.message);
                    }
                } catch (error) {
                    console.error('加载优惠券异常:', error);
                }
            },

            async loadPreview() {
                if (!this.selectedAddressId) {
                    this.previewData = null;
                    return;
                }

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
                    const result = await response.json();

                    if (result.success && result.data) {
                        this.previewData = result.data;
                    } else {
                        console.error('加载预览失败:', result.message);
                        alert(result.message || '加载预览失败，请重试');
                    }
                } catch (error) {
                    console.error('加载预览异常:', error);
                    alert('加载预览失败，请检查网络连接');
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
                        // TEMP_DEMO_PAYMENT: member5 合入前先跳转到临时模拟支付页。
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
