Vue.component('$componentname$', {
    props: ['input'],
    template: $template$,
    mounted: function () {
        this.$on('value_changed', function (data) {
            this.$parent.$emit('value_changed', data);
        });
        if ((this.input.required==undefined ? false : this.input.required)) {
            $($(this.$el).find('input,select,textarea')).each(function (index, input) {
                $(input).prop('required', true);
            });
        }
    },
    computed: {
        columns: function () {
            if (this.input.form_columns != undefined && this.input.form_columns != null) {
                return 'col-md-' + this.input.form_columns.toString();
            } else {
                return '';
            }
        },
        typeclass: function () { return 'fb-' + this.input.type; },
        fieldclass: function () { return 'field-' + this.input.name; },
        value: function () { return this.$children[0].value; },
        fieldName: function () { return this.input.name; },
        isValid: function () {
            if ((this.input.required==undefined ? false : true)) {
                if (value == null)
                    return false;
                else if (value.toString() == '')
                    return false;
                else if (_.isArray(value))
                    return value.length > 0;
            }
            return true;
        }
    }
});