import { loadModule, createCJSModule } from 'vue-loader';
import * as vue from 'vue';

const options = {
    addStyle: () => { },
    moduleCache: {
        vue: vue
    },
    handleModule: (type, source, path, options) => {
        console.log('handleModule');
        console.log(type);
        console.log(source);
        console.log(path);
        console.log(options);
    },
    getFile: async (url) => {
        console.log(url);
        switch (url) {
            case 'notification.vue':
                return Promise.resolve(`<script setup>
import { ref, watch } from 'vue';
import {test_button} from '/buttons/*.vue';
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
</template>`);
                break;

            default:
                if (url.endsWith('.vue') || url.endsWith('.mjs')) {
                    url = url.substring(0, url.length - 4) + '.js';
                }
                const res = await fetch(url);
                if (!res.ok)
                    throw Object.assign(new Error(url + ' ' + res.statusText), { res });
                return await res.text();
                break;
        }
    }
};
const testLoader = vue.defineAsyncComponent(() => loadModule('notification.vue', options));
export default testLoader;