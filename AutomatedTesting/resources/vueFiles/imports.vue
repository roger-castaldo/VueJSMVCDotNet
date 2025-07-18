<template>
    <div v-bind:class="['notification','is-'+(type==null ? 'primary' : type),(light ? 'is-light' : '')]">
        {{Message}}
        <slot/>
    </div>
</template>

<script>
    import { imp1 } from '/imp1.vue';
    import { imp2, imp3 } from '/components/';
    import { imp4 } from '../imps.js';
    import { imp5 } from './imps.js';
    import imp11 from 'imp11.vue';

    const { imp6 } = await import('/imp6.vue');
    const { imp7, imp8 } = await import('/components2/');
    const { imp9} = await import('../imps9.js');
    const { imp10 } = await import('./imps9.js');
    const imp12 = await import('imp12.vue');

export default {
    props:['type','message','light'],
    computed:{
        Message:{
            get:function(){
                if (this.message==null){
                    return this.message;
                }else if (this.message.contains('.')){
                    return this.Translate(this.message);
                }else{
                    return this.message;
                }
            }
        }
    }
};
</script>