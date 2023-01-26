<template>
    <base_button icon="plus" v-on:click="$emit('click')" v-bind:disabled="disabled" v-bind:addonclass="addonclass" title="components.common.button.Add" :hidemobile="hidemobile" :hidetablet="hidetablet" :hidedesktop="hidedesktop"/>
</template>
<script>
    import { button as base_button } from './button.vue';

export default {   
    emits:['click'],
    props:['disabled','addonclass','hidemobile','hidetablet','hidedesktop']
};
</script>
