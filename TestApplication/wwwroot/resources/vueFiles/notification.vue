<template>
    <div v-bind:class="['notification','is-'+(type==null ? 'primary' : type),(light ? 'is-light' : '')]">
        <slot>
            {{Message}}
        </slot>
    </div>
</template>

<script>
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