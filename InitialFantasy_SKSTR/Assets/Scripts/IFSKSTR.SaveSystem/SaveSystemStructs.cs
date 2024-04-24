using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Leguar.TotalJSON;
using Unity.VisualScripting;
using UnityEngine.Serialization;

namespace IFSKSTR.SaveSystem
{
    /*
    What does a field of my script need to be in order to be serialized?

    Be public, or have [SerializeField] attribute
    Not be static
    Not be const
    Not be readonly
    The fieldtype needs to be of a type that we can serialize.
    Which fieldtypes can we serialize?

    Custom non abstract classes with [Serializable] attribute.
    Custom structs with [Serializable] attribute. (new in Unity4.5)
    References to objects that derive from UnityEngine.Object
    Primitive data types (int,float,double,bool,string,etc)
    Array of a fieldtype we can serialize
    List<T> of a fieldtype we can serialize
     */
    public class TypeConduitPair
    {
        public Type Type;
        public Conduit Conduit;
        
        
        public TypeConduitPair(Type t, Conduit value)
        {
            Type = t;
            Conduit = value;
        }

        public TypeConduitPair(Type t, in Conduit.Get get, in Conduit.Set set)
        {
            Type = t;
            Conduit = new Conduit(in get, in set);
        }

        public override string ToString()
        {
            return "(" + Type + ", " + Conduit + ")";
        }
    }

    public class ConduitValuePair
    {
        public TypeConduitPair ConduitPair { get; set; }
        public TypeValuePair ValuePair { get; set; }
        public bool IsValueValid;
        public bool IsConduitValid;
        
        public ConduitValuePair(TypeConduitPair conduitPair)
        {
            ValuePair = null;
            IsValueValid = false;
            
            ConduitPair = conduitPair;
            IsConduitValid = true;
        }
        public ConduitValuePair(TypeValuePair typeValuePair)
        {
            ValuePair = typeValuePair;
            IsValueValid = true;
            
            ConduitPair = null;
            IsConduitValid = false;
        }

        public void AddValuePair(TypeValuePair typeValuePair)
        {
            ValuePair = typeValuePair;
            IsValueValid = true;
        }
        public void AddConduitPair(TypeConduitPair typeConduitPair)
        {
            ConduitPair = typeConduitPair;
            IsConduitValid = true;
        }
        
        
        public void InvalidateConduit()
        {
            IsConduitValid = false;
            ConduitPair = null;
        }
        public void InvalidateValue()
        {
            IsValueValid = false;
            ValuePair = null;
        }

        public override string ToString()
        {
            string s = "";
            if (IsConduitValid) s += "conduit: " + ConduitPair + ", ";
            if (IsValueValid) s += "value:" + ValuePair;
            return "["+ s + "]";
        }
    }

    [Serializable]
    public class TypeValuePair : IJsonSerializable
    {
        [SerializeField] private int type;
        [NonSerialized] public Type Type;
        [SerializeField] private string value;
        [NonSerialized] public object Value;
        private const string TypeKey = "type";
        private const string ValueKey = "value";
        
        public TypeValuePair()
        {
            Type = null;
            Value = null;
            type = 0;
            value = null;
        }
        public TypeValuePair(Type t, object v)
        {
            type = 0;
            value = null;
            Type = t;
            Value = v;
        }
        public override string ToString()
        {
            return "(type: " + Type + "(" + type + ") value: " + Value + "(" + value + "))";
        }
        

        public void SerializeValue()
        {
            TypeCode typeCode = Type.GetTypeCode(Type);
            JValue jValue;
            if (new[] {
                    TypeCode.SByte, TypeCode.Byte, TypeCode.Int16, TypeCode.UInt16, TypeCode.Int32, TypeCode.UInt32,
                    TypeCode.Int64, TypeCode.UInt64, TypeCode.Single, TypeCode.Double, TypeCode.Decimal
                }.Contains(typeCode)) //is a number
            { 
                string s = Value.ToString();
                jValue = new JNumber(s);
            }
            else if (new[] {TypeCode.Char, TypeCode.String, TypeCode.DateTime}.Contains(typeCode)){
                jValue = new JString((string)Value);
            }else if (typeCode is TypeCode.Empty or TypeCode.DBNull)
            {
                jValue = new JNull();
            }else if (typeCode is TypeCode.Boolean)
            {
                jValue = new JBoolean((bool)Value);
            }
            else //is object
            {
                jValue = JSON.Serialize(Value);
            }

            this.value = jValue.CreateString();
        }
        
        public void DeserializeValue()
        {
            TypeCode typeCode = Type.GetTypeCode(Type);
            object obj;
            const string valueKey = "value";
            JSON json;
            if (value != null)
            {
                 json = JSON.ParseString("{\""+valueKey+"\":" + value + "}");
            }
            else
            {
                json = new JSON();
            }
            if (new[] {
                    TypeCode.SByte, TypeCode.Byte, TypeCode.Int16, TypeCode.UInt16, TypeCode.Int32, TypeCode.UInt32,
                    TypeCode.Int64, TypeCode.UInt64, TypeCode.Single, TypeCode.Double, TypeCode.Decimal
                }.Contains(typeCode)) //is a number
            {
                JNumber jNumber = json.GetJNumber(valueKey);
                if (typeCode is TypeCode.Single or TypeCode.Double or TypeCode.Decimal)
                {
                    obj = jNumber.AsDouble();
                }
                else if(typeCode is TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 )
                {
                    obj = jNumber.AsULong();
                }
                else
                {
                    obj = jNumber.AsLong();
                }
                
            }
            else if (new[] {TypeCode.Char, TypeCode.String, TypeCode.DateTime}.Contains(typeCode))
            {
                obj = json.GetString(ValueKey);
            }else if (typeCode is TypeCode.Empty or TypeCode.DBNull)
            {
                obj = null;
            }else if (typeCode is TypeCode.Boolean)
            {
                obj = json.GetBool(ValueKey);
            }
            else //is object
            {
                obj = json.Deserialize<object>();
            }
            Value = Convert.ChangeType(obj, Type);
        }

        private static Type GetType(int code)
        {
            if (code == -1) code = 0;
            TypeCode tc = (TypeCode)code;
            return Type.GetType("System." + Enum.GetName(typeof(TypeCode), tc));
        }

        private static int GetTypeAsInt(Type type)
        {
            TypeCode tc = Type.GetTypeCode(type);
            return (int)tc;
        }
        
        public void JsonSerialize()
        {
            SerializeValue();
            type = GetTypeAsInt(Type);
        }
        public void JsonDeserialize()
        {
            Type = GetType(type);
            DeserializeValue();
        }
    }

    [Serializable]
    public class ObjectSaveData : IJsonSerializable //: ISerializationCallbackReceiver
    {
        public string id;
        public int hash;
        public List<TypeValuePair> typeValuePairs;

        public ObjectSaveData()
        {
            id = null;
            typeValuePairs = null;
            hash = 0;
        }
        public ObjectSaveData(string objectIdentifier, List<TypeValuePair> valuePairs)
        {
            id = objectIdentifier;
            typeValuePairs = valuePairs;
            hash = typeValuePairs.GetHashCode();
        }

        public override string ToString()
        {
            string result = "";
            foreach (var typeValue in typeValuePairs)
            {
                result += "(" + typeValue + "), ";
            }

            return "(ID: " + id + ", Hash: " + hash + ", Values: " + result + ")";
        }

        public void JsonSerialize()
        {
            foreach (var tvp in typeValuePairs)
            {
                tvp.JsonSerialize();
            }
            hash = typeValuePairs.GetHashCode();
            //Debug.Log(string.Join(",", typeValuePairs));

        }

        public void JsonDeserialize()
        {
            foreach (var tvp in typeValuePairs)
            {
                tvp.JsonDeserialize();
            }
        }
    }
    
    [Serializable]
    public struct ListWrapper<T>: IJsonSerializable where T: IJsonSerializable
    {
        public List<T> values;
        public ListWrapper(List<T> values)
        {
            this.values = values;
        }

        public void JsonSerialize()
        {
            foreach (var value in values)
            {
                value.JsonSerialize();
            }
        }

        public void JsonDeserialize()
        {
            foreach (var value in values)
            {
                value.JsonDeserialize();
            }
        }
    }
    
}