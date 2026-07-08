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
                    // 并行加载地址、优惠券和预览数据
                    await Promise.all([
                        this.loadAddresses(),
                        this.loadCoupons(),
                        this.loadPreview()
                    ]);

                    // 自动选择默认地址
                    const defaultAddr = this.addresses.find(a => a.isDefault);
                    if (defaultAddr) {
                        this.selectedAddressId = defaultAddr.addressId;
                    } else if (this.addresses.length > 0) {
                        this.selectedAddressId = this.addresses[0].addressId;
                    }

                    // 重新计算预览（如果有优惠券或地址变化，预览会变化）
                    // 但预览在 loadPreview 中已用当前优惠券计算，无需重复
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
                        // 只显示未使用的优惠券
                        this.coupons = result.data.filter(c => c.status === 0);
                    } else {
                        console.warn('加载优惠券失败:', result.message);
                    }
                } catch (error) {
                    console.error('加载优惠券异常:', error);
                }
            },

            async loadPreview() {
                try {
                    const requestBody = {
                        addressId: this.selectedAddressId || 0,
                        userCouponId: this.selectedCouponId || null,
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
                        userCouponId: this.selectedCouponId || null,
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
                        // 跳转到支付页（Member5 负责）
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

            formatCouponInfo(coupon) {
                // 假设 coupon 包含 couponTemplateId 和金额信息，具体字段以实际 DTO 为准
                // 这里做兼容处理
                return '可用优惠券';
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
                // 优惠券变化时重新计算金额
                if (!this.loading) {
                    this.loadPreview();
                }
            },
            remark() {
                // 备注变化不影响金额，但如果需要可以在这里防抖更新预览
            }
        }
    }).mount('#checkoutApp');
})();
