<template>
    <button class="button is-rounded is-small" v-bind:class="[disabled ? 'disabled' : '',addonclass==null ? '' : addonclass,type==null ? 'is-primary' : 'is-'+type.toString(),hidemobile?'is-hidden-mobile':'',hidetablet?'is-hidden-tablet-only':'',hidedesktop?'is-hidden-desktop is-hidden-widescreen':'']" v-bind:disabled="disabled" v-on:click="$emit('click')">
        <span class="icon is-small" v-if="icon!=null">
            <icon :icon="icon"/>
        </span>
        <span v-if="title!=null">{{fixedTitle}}</span>
    </button>
</template>
<script>
    import '../icon.vue';

    export default {
        props: ['icon', 'disabled', 'type', 'title', 'addonclass', 'hidemobile', 'hidetablet', 'hidedesktop'],
        emits: ['click'],
        computed: {
            fixedTitle: {
                get: function () {
                    if (this.title != null) {
                        if (this.title.contains('.')) {
                            return this.Translate(this.title);
                        }
                    }
                    return this.title;
                }
            }
        }
    };
</script>
