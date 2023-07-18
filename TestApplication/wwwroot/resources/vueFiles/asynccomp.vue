<template>
    <slot v-if="resolve!=null" name="resolved" v-bind="resolve"></slot>
    <slot v-if="reject!=null" name="rejected" v-bind="reject"></slot>
</template>

<script>
    export default {
        props: ['promise', 'resultname'],
            data: function () {
                return { resolve: null, reject: null };
            },
            watch: {
                promise: function (val) {
                    if (val != null && val instanceof Promise) {
                        this.handlePromise();
                    } else {
                        this.resolve = null;
                        this.reject = null;
                    }
                }
            },
            methods: {
                handlePromise() {
                    var view = this;
                    this.promise.then(
                        result => {
                            if (view.resultname != undefined && view.resultname != null) {
                                var tmp = {};
                                tmp[view.resultname] = result;
                                view.resolve = tmp;
                            } else {
                                view.resolve = result;
                            }
                            view.reject = null;
                        },
                        result => {
                            if (view.resultname != undefined && view.resultname != null) {
                                var tmp = {};
                                tmp[view.resultname] = result;
                                view.reject = tmp;
                            } else {
                                view.reject = result;
                            }
                            view.resolve = null;
                        }
                    );
                }
            },
            mounted: function () {
                if (this.promise != null && this.promise instanceof Promise) {
                    this.handlePromise();
                } else {
                    this.resolve = null;
                    this.reject = null;
                }
            }
    }
</script>