<template>
    <i :class="clazz"/>
</template>

<script>
    export default {
        props: ['icon'],
        computed: {
            clazz: {
                get: function () {
                    var ret = 'fa-solid';
                    ret += (this.icon == undefined || this.icon == null ? '' : (this.icon.indexOf('fa-') == -1 ? ' fa-' : ' ') + this.icon);
                    return ret;
                }
            }
        }
    }
</script>