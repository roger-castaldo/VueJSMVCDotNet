<template>
    <div v-bind:class="['notification','is-'+(type==null ? 'primary' : type),(light ? 'is-light' : '')]">
        {{Message}}
        <slot/>
    </div>
</template>

<script setup>
    import { computed } from 'vue';
    import { mPerson } from '/models/mPerson.js';
    const props = defineProps(['type','message','light']);

    const group = computed(async () => {
        var { mGroup } = await import('/models/mGroup.js');

        return mGroup;
    });
</script>

<style scoped>
    .notification{
        font-size:120px;
    }
</style>