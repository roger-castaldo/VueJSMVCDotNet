<script setup>
    import { ref, watch } from 'vue';
    // variable
    const msg = ref('Hello!');

    const props = defineProps({
        foo: String
    });

    // functions
    function log() {
        console.log(msg)
    }

    watch(() => props.foo, (currentValue, oldValue) => {
        console.log(currentValue);
        console.log(oldValue);
    });
</script>

<template>
    <button @click="log">{{ msg }},{{foo}}</button>
</template>