using Microsoft.AspNetCore.Mvc;
using System;
using System.Reflection;
using static Petaframework.TypeDef;

namespace Petaframework
{
    public class FormCaptionAttribute : System.Attribute
    {
        public static String ResourceFile { get; set; }

        public Boolean PrimaryKey { get; set; }
        public String LabelText { get; set; }
        public String Tooltip { get; set; }

        public Boolean Required { get; set; }

        private String _requiredMessage = " ";
        public String RequiredMessage { get { return _requiredMessage; } set { _requiredMessage = value; } }

        public InputType InputType { get; set; } = InputType.None;

        public int ShowOrder { get; set; } = 0;
        public string GroupBy { get; set; }

        public Boolean IsImplicit { get; set; }
        public Boolean IsSubform { get { return !String.IsNullOrWhiteSpace(SubformEntityName); } }
        public String SubformEntityName { get; set; }

        public Boolean HasPasswordMask { get; set; }

        public FormCaptionAttribute() { }

        public FormCaptionAttribute(String labelText)
        {
            this.LabelText = labelText;
        }

        /// <summary>
        /// Property name that mirrored. That mirror property refers a Database Column
        /// </summary>
        public String MirroredOf { get; set; }

        /// <summary>
        /// Optional. Indicates that this field will be presented to users who have access to the process as searchable  on determinate states. Default Value: ReadableFieldType.Never
        /// </summary>
        public ReadableFieldType ReadableType { get; set; } = ReadableFieldType.Never;

        /// <summary>
        /// If the property represents a Entity Object set the name here to associate with the Framework
        /// </summary>
        public String EntityName { get; set; }

        /// <summary>
        /// Optional. Indicates the maximum number of characters in a field.
        /// </summary>
        public int MaxLength { get; set; }

        /// <summary>
        /// Optional.Indicates the field mask displayed to the user.
        /// </summary>
        public MaskTypeEnum MaskType { get; set; }

        /// <summary>
        /// a - Represents an alpha character (A-Z,a-z) |
        /// 9 - Represents a numeric character (0-9) |
        /// * - Represents an alphanumeric character (A-Z,a-z,0-9) |
        /// view more in https://plugins.jquery.com/maskedinput/
        /// </summary>
        public string CustomMask { get; set; }

        /// <summary>
        /// By default, refers to the Method name that returns options to respective property, or if OptionsType is ClientSide, refers to the Context Property Name to generate options
        /// </summary>
        public string OptionsContext { get; set; }

        /// <summary>
        /// Optional. Indicates that this property will be displayed as the User Text in a list field
        /// </summary>
        public Boolean TextLabelOnList { get; set; } = false;

        public String GetMask()
        {
            const string NUMBER_TOKEN = "9";
            const string ALPHA_TOKEN = "a";
            const string ALPHANUMERIC_TOKEN = "*";

            string mask = null;
            switch (MaskType)
            {
                case MaskTypeEnum.date:
                    mask = "00/00/0000".Replace("0", NUMBER_TOKEN);
                    break;
                case MaskTypeEnum.time:
                    mask = "00:00:00".Replace("0", NUMBER_TOKEN);
                    break;
                case MaskTypeEnum.date_time:
                    mask = "00/00/0000 00:00".Replace("0", NUMBER_TOKEN);
                    break;
                case MaskTypeEnum.cep:
                    mask = "00000-000".Replace("0", NUMBER_TOKEN);
                    break;
                case MaskTypeEnum.phone:
                    mask = "0000-0000".Replace("0", NUMBER_TOKEN);
                    break;
                case MaskTypeEnum.phone_with_ddd:
                    mask = "BRPhone";//"(00) 0000-0000".Replace("0", NUMBER_TOKEN);
                    break;
                case MaskTypeEnum.cpf:
                    mask = "000.000.000-00".Replace("0", NUMBER_TOKEN);
                    break;
                case MaskTypeEnum.cnpj:
                    mask = "00.000.000/0000-00".Replace("0", NUMBER_TOKEN);
                    break;

                case MaskTypeEnum.cpf_cnpj:
                    mask = "BRcpf_cnpj";// "00.000.000/0000-00".Replace("0", NUMBER_TOKEN);
                    break;
                case MaskTypeEnum.money:
                    mask = "money";
                    break;
                case MaskTypeEnum.integer:
                    mask = "int";
                    break;
                case MaskTypeEnum.placa_veicular:
                    mask = "aaa 0*000".Replace("0", NUMBER_TOKEN).Replace("a", ALPHA_TOKEN).Replace("*", ALPHANUMERIC_TOKEN);
                    break;
                case MaskTypeEnum.email:
                    mask = "email";
                    break;
                default:
                    if (!String.IsNullOrWhiteSpace(CustomMask))
                        mask = CustomMask;
                    break;
            }
            return mask;
        }

        private RequestMode optionsType = RequestMode.ServerSide;
        /// <summary>
        /// Method type for request the options. Default: ServerSide
        /// </summary>
        public RequestMode OptionsType
        {
            get { return optionsType; }
            set { optionsType = value; }
        }

        internal static PropertyInfo GetPrimaryKey(object frm)
        {
            foreach (var prop in frm.GetType().GetProperties())
            {
                object[] attrs = prop.GetCustomAttributes(true);
                if (attrs.Length == 0)
                    attrs = prop.DeclaringType.GetCustomAttributes(true);
                foreach (var attr in attrs)
                {
                    var model = attr as ModelMetadataTypeAttribute;
                    if (model != null && model.MetadataType != null)
                        //TODO Net Core
                        foreach (var metadata in model.MetadataType.GetProperties())
                        {
                            foreach (var metaAttr in metadata.GetCustomAttributes(true))
                            {
                                FormCaptionAttribute authAttr = metaAttr as FormCaptionAttribute;
                                if (authAttr != null && authAttr.PrimaryKey)
                                {
                                    return metadata;
                                }
                            }
                        }
                }
            }
            return null;
        }
    }

    public enum MaskTypeEnum
    {
        _NONE,
        date,
        time,
        date_time,
        cep,
        phone,
        phone_with_ddd,
        cpf,
        cnpj,
        money,
        ip_address,
        percent,
        integer,
        placa_veicular,
        cpf_cnpj,
        email
    }

    public enum ElementType
    {
        container,
        color,
        date,
        datetime_local,
        email,
        month,
        number,
        range,
        search,
        tel,
        time,
        url,
        week,
        text,
        password,
        submit,
        fieldset,
        reset,
        hr,
        radio,
        hidden,
        checkbox,
        radiobuttons,
        checkboxes,
        subform,
        select,
        uploader,
        textarea,
        selectmultiple
    }

    public enum ReadableFieldType
    {
        Never,
        Always,
        WhenFilled,
        WhenFinalized
    }
}
