Vue.component('$componentname$', {
    template: '<div><label v-bind:for="name">{{label}}</label><br/><input v-bind:type="subtype" class="form-control" v-bind:name="name"></div>',
    props: ['name', 'label', 'subtype'],
    mounted: function () {
        var vue = this;
        $($(this.$el).find('input')).on('input', function (event) {
            vue.$parent.$emit('value_changed', { name: vue.name, value: $(event.target).val() });
        });
    },
    computed: {
        value: function () {
            return $($(this.$el).find('input')[0]).val();
        }
    }
});