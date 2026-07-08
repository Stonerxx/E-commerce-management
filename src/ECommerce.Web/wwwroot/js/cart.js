(function () {
    const { createApp } = Vue;

    createApp({
        data() {
            return {
                loading: false,
                items: [],
                selectedTotal: 0,
                selectedCount: 0,
                pageIndex: 1,
                pageSize: 20,
                totalCount: 0,
                totalPages: 1,
                filters: {
                    status: ''
                }
            };
        },
        computed: {
            allSelected: {
                get() {
                    return this.items.length > 0 && this.items.every(item => item.selected);
                },
                set(value) {
                    this.items.forEach(item => {
                        if (item.selected !== value) {
                            item.selected = value;
                            this.updateItem(item);
                        }
                    });
                }
            }
        },
        mounted() {
            this.loadCart();
        },
        methods: {
            async loadCart() {
                this.loading = true;
                try {
                    const response = await fetch('/api/v1/cart', {
                        headers: { 'Accept': 'application/json' }
                    });
                    const result = await response.json();

                    if (result.success && result.data) {
                        this.items = result.data.items || [];
                        this.calculateTotals();
                    } else {
                        console.error('加载购物车失败:', result.message);
                    }
                } catch (error) {
                    console.error('加载购物车异常:', error);
                } finally {
                    this.loading = false;
                }
            },

            calculateTotals() {
                const selected = this.items.filter(item => item.selected);
                this.selectedCount = selected.length;
                this.selectedTotal = selected.reduce((sum, item) => sum + (item.unitPrice * item.quantity), 0);
            },

            async updateItem(item) {
                try {
                    const response = await fetch(`/api/v1/cart/items/${item.cartItemId}`, {
                        method: 'PUT',
                        headers: {
                            'Content-Type': 'application/json',
                            'Accept': 'application/json'
                        },
                        body: JSON.stringify({
                            quantity: item.quantity,
                            selected: item.selected
                        })
                    });
                    const result = await response.json();

                    if (result.success) {
                        this.calculateTotals();
                    } else {
                        // 回滚
                        await this.loadCart();
                        alert(result.message || '更新失败');
                    }
                } catch (error) {
                    console.error('更新购物车异常:', error);
                    await this.loadCart();
                }
            },

            async removeItem(cartItemId) {
                if (!confirm('确定要删除这个商品吗？')) return;

                try {
                    const response = await fetch(`/api/v1/cart/items/${cartItemId}`, {
                        method: 'DELETE',
                        headers: { 'Accept': 'application/json' }
                    });
                    const result = await response.json();

                    if (result.success) {
                        this.items = this.items.filter(item => item.cartItemId !== cartItemId);
                        this.calculateTotals();
                    } else {
                        alert(result.message || '删除失败');
                    }
                } catch (error) {
                    console.error('删除购物车项异常:', error);
                    alert('删除失败，请稍后重试');
                }
            },

            async clearCart() {
                if (!confirm('确定要清空购物车吗？')) return;

                try {
                    const response = await fetch('/api/v1/cart', {
                        method: 'DELETE',
                        headers: { 'Accept': 'application/json' }
                    });
                    const result = await response.json();

                    if (result.success) {
                        this.items = [];
                        this.calculateTotals();
                    } else {
                        alert(result.message || '清空失败');
                    }
                } catch (error) {
                    console.error('清空购物车异常:', error);
                    alert('清空失败，请稍后重试');
                }
            },

            increaseQuantity(item) {
                item.quantity += 1;
                this.updateItem(item);
            },

            decreaseQuantity(item) {
                if (item.quantity <= 1) return;
                item.quantity -= 1;
                this.updateItem(item);
            },

            checkout() {
                const selectedIds = this.items.filter(item => item.selected).map(item => item.cartItemId);
                if (selectedIds.length === 0) {
                    alert('请至少选择一件商品');
                    return;
                }

                // 跳转到订单确认页，携带选中的购物车项 ID
                const params = new URLSearchParams();
                selectedIds.forEach(id => params.append('cartItemIds', id));
                window.location.href = `/orders/create?${params.toString()}`;
            }
        }
    }).mount('#cartApp');
})();
