<template>
    <Button icon="plus" v-on:click="$emit('click')" v-bind:disabled="disabled" v-bind:addonclass="addonclass" title="components.common.button.Add" :hidemobile="hidemobile" :hidetablet="hidetablet" :hidedesktop="hidedesktop"/>
</template>
<script>
    import Button from './button.vue';

    export default {   
        components: { Button },
        emits:['click'],
        props:['disabled','addonclass','hidemobile','hidetablet','hidedesktop']
    };
</script>
