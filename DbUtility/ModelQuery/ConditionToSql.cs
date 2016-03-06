using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace FireflySoft.DbUtility.ModelQuery
{
    /// <summary>
    /// ��QueryCondition����ת��ΪSQLServer��ѯ���
    /// </summary>
    public class ConditionToSql
    {
        /// <summary>
        /// ת����ѯ��������ΪSql�ı�
        /// </summary>
        /// <param name="conditionArray"></param>
        /// <returns></returns>
        public static string ToSqlText(QueryCondition[] conditionArray)
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (conditionArray != null && conditionArray.Length > 0)
            {
                Dictionary<string, int> parameterDictionary = new Dictionary<string, int>();
                for (int i = 0; i < conditionArray.Length; i++)
                {
                    QueryCondition conditon = conditionArray[i];
                    stringBuilder.Append(ConditionToSql.ToSubSqlText(conditon, parameterDictionary));
                }

                parameterDictionary.Clear();
                return ConditionToSql.FixSQLText(stringBuilder.ToString());
            }

            return string.Empty;
        }

        /// <summary>
        /// ת����ѯ��������ΪSql����
        /// </summary>
        /// <param name="conditionArray"></param>
        /// <returns></returns>
        public static SqlParameter[] ToSqlParas(QueryCondition[] conditionArray)
        {
            List<SqlParameter> parameterList = new List<SqlParameter>();

            if (conditionArray != null && conditionArray.Length > 0)
            {
                Dictionary<string, int> parameterDictionary = new Dictionary<string, int>();
                for (int i = 0; i < conditionArray.Length; i++)
                {
                    QueryCondition conditon = conditionArray[i];
                    ConditionToSql.ToSubSqlParas(conditon, parameterList, parameterDictionary);
                }

                parameterDictionary.Clear();
            }

            return parameterList.ToArray();
        }

        /// <summary>
        /// ����SQL��䣬ȥ��SQL���ǰ��������������
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static string FixSQLText(string str)
        {
            string text = str.Trim();
            if (text.StartsWith(LinkType.And.ToString()))
            {
                text = text.Substring(3);
            }
            else
            {
                if (text.StartsWith(LinkType.Or.ToString()))
                {
                    text = text.Substring(2);
                }
            }
            return text.Trim();
        }

        /// <summary>
        /// ת��������ѯ����ΪSql�ı�
        /// </summary>
        /// <param name="conditon"></param>
        /// <param name="parameterDictionary"></param>
        /// <returns></returns>
        private static string ToSubSqlText(QueryCondition conditon, Dictionary<string, int> parameterDictionary)
        {
            if (conditon != null && ConditionToSql.CheckConditionIsValid(conditon))
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(" ");
                stringBuilder.Append(ConditionToSql.GetLinkType(conditon.LinkType));
                stringBuilder.Append(" ");

                // ������Ӽ���ѯ����������������Ӽ���ѯ����
                if (conditon.SubQuery != null && conditon.SubQuery.Length > 0)
                {
                    string subSqlText = string.Empty;
                    QueryCondition[] subQuery = conditon.SubQuery;

                    for (int i = 0; i < subQuery.Length; i++)
                    {
                        QueryCondition subConditon = subQuery[i];
                        subSqlText += ConditionToSql.ToSubSqlText(subConditon, parameterDictionary);
                    }

                    if (!string.IsNullOrEmpty(subSqlText))
                    {
                        stringBuilder.Append("(");
                        stringBuilder.Append(ConditionToSql.FixSQLText(subSqlText));
                        stringBuilder.Append(")");
                    }
                }
                else
                {
                    stringBuilder.Append(conditon.Property.ToString());
                    stringBuilder.Append(" ");
                    stringBuilder.Append(ConditionToSql.GetCompareType(conditon));
                    stringBuilder.Append(" ");

                    if (conditon.CompareType == CompareType.In || conditon.CompareType == CompareType.NotIn)
                    {
                        stringBuilder.Append("(");
                    }

                    stringBuilder.Append(ConditionToSql.GetParameterName(conditon, parameterDictionary));

                    if (conditon.CompareType == CompareType.In || conditon.CompareType == CompareType.NotIn)
                    {
                        stringBuilder.Append(")");
                    }
                }

                return stringBuilder.ToString();
            }

            return string.Empty;
        }

        /// <summary>
        /// ת��������ѯ����ΪSql����
        /// </summary>
        /// <param name="conditon"></param>
        /// <param name="parameterList"></param>
        /// <param name="parameterDictionary"></param>
        private static void ToSubSqlParas(QueryCondition conditon, List<SqlParameter> parameterList, Dictionary<string, int> parameterDictionary)
        {
            if (conditon != null && ConditionToSql.CheckConditionIsValid(conditon))
            {
                if (conditon.SubQuery != null && conditon.SubQuery.Length > 0)
                {
                    QueryCondition[] subQuery = conditon.SubQuery;
                    for (int i = 0; i < subQuery.Length; i++)
                    {
                        QueryCondition subConditon = subQuery[i];
                        ConditionToSql.ToSubSqlParas(subConditon, parameterList, parameterDictionary);
                    }
                }

                if (!string.IsNullOrEmpty(conditon.Property.ToString()))
                {
                    ConditionToSql.GetParameterObject(conditon, parameterList, parameterDictionary);
                }
            }
        }

        /// <summary>
        /// ��ȡ������ѯ������Ӧ�Ĳ�������
        /// </summary>
        /// <param name="conditon"></param>
        /// <param name="parameterDictionary"></param>
        /// <returns></returns>
        private static string GetParameterName(QueryCondition conditon, Dictionary<string, int> parameterDictionary)
        {
            var parameterName = string.Empty;
            var conditionName = conditon.ConditionName.ToString();
            bool isArray = false;
            if (conditon.Value != null)
            {
                isArray = conditon.Value.GetType().IsArray;
            }

            if (isArray)
            {
                var conditonValueEnum = (System.Collections.IEnumerable)conditon.Value;

                foreach (var v in conditonValueEnum)
                {
                    var tmpParameterName = "@" + conditionName;
                    tmpParameterName = GetUniqueParameterName(tmpParameterName, parameterDictionary);

                    parameterName += tmpParameterName + ",";
                }

                if (!string.IsNullOrEmpty(parameterName))
                {
                    parameterName = parameterName.Substring(0, parameterName.Length - 1);
                }
            }
            else
            {
                if (conditon.CompareType == CompareType.GET || conditon.CompareType == CompareType.GT)
                {
                    parameterName = "@min" + conditionName;
                }
                else if (conditon.CompareType == CompareType.LT || conditon.CompareType == CompareType.LET)
                {
                    parameterName = "@max" + conditionName;
                }
                else
                {
                    parameterName = "@" + conditionName;
                }

                parameterName = GetUniqueParameterName(parameterName, parameterDictionary);
            }

            return parameterName;
        }

        /// <summary>
        /// ��ȡΨһ�Ĳ�����
        /// </summary>
        /// <param name="parameterName"></param>
        /// <param name="parameterDictionary"></param>
        /// <returns></returns>
        private static string GetUniqueParameterName(string parameterName, Dictionary<string, int> parameterDictionary)
        {
            // ����������Ѿ����ڣ����ڲ�����֮����ӱ��
            if (parameterDictionary.ContainsKey(parameterName))
            {
                parameterDictionary[parameterName] = parameterDictionary[parameterName] + 1;
                parameterName += parameterDictionary[parameterName].ToString();
            }
            else
            {
                parameterDictionary.Add(parameterName, 0);
            }

            return parameterName;
        }

        /// <summary>
        /// ��ȡ����������Ӧ�Ĳ���ֵ����
        /// </summary>
        /// <param name="conditon"></param>
        /// <param name="parameterList"></param>
        /// <param name="parameterDictionary"></param>
        /// <returns></returns>
        private static string GetParameterObject(QueryCondition conditon, List<SqlParameter> parameterList, Dictionary<string, int> parameterDictionary)
        {
            string parameterName = string.Empty;
            string conditionName = conditon.ConditionName.ToString();
            bool isArray = false;
            if (conditon.Value != null)
            {
                isArray = conditon.Value.GetType().IsArray;
            }

            if (isArray)
            {
                var conditonValueEnum = (System.Collections.IEnumerable)conditon.Value;

                foreach (var v in conditonValueEnum)
                {
                    var tmpParameterName = "@" + conditionName;
                    tmpParameterName = GetUniqueParameterName(tmpParameterName, parameterDictionary);

                    parameterList.Add(ConditionToSql.CreateParameterObject(tmpParameterName, v));
                }
            }
            else
            {
                if (conditon.CompareType == CompareType.GET || conditon.CompareType == CompareType.GT)
                {
                    parameterName = "@min" + conditionName;
                }
                else
                {
                    if (conditon.CompareType == CompareType.LT || conditon.CompareType == CompareType.LET)
                    {
                        parameterName = "@max" + conditionName;
                    }
                    else
                    {
                        parameterName = "@" + conditionName;
                    }
                }

                parameterName = GetUniqueParameterName(parameterName, parameterDictionary);

                parameterList.Add(ConditionToSql.CreateParameterObject(parameterName, conditon.Value));
            }

            return parameterName;
        }

        /// <summary>
        /// ��ȡ����֮�����������
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static string GetLinkType(LinkType type)
        {
            switch (type)
            {
                case LinkType.And:
                    return "And";
                case LinkType.Or:
                    return "Or";
            }

            return string.Empty;
        }

        /// <summary>
        /// ��ȡ�����ıȽ�����
        /// </summary>
        /// <param name="conditon"></param>
        /// <returns></returns>
        private static string GetCompareType(QueryCondition conditon)
        {
            if (conditon.Value == null)
            {
                if (CompareType.Equal == conditon.CompareType)
                {
                    conditon.CompareType = CompareType.Is;
                }
                else
                {
                    if (CompareType.NotEqual == conditon.CompareType)
                    {
                        conditon.CompareType = CompareType.IsNot;
                    }
                }
            }
            switch (conditon.CompareType)
            {
                case CompareType.Like:
                    return "like";
                case CompareType.GET:
                    return ">=";
                case CompareType.GT:
                    return ">";
                case CompareType.LT:
                    return "<";
                case CompareType.LET:
                    return "<=";
                case CompareType.Equal:
                    return "=";
                case CompareType.NotEqual:
                    return "<>";
                case CompareType.In:
                    return "in";
                case CompareType.NotIn:
                    return "not in";
                case CompareType.Is:
                    return "is";
                case CompareType.IsNot:
                    return "is not";
                default:
                    return "=";
            }
        }

        /// <summary>
        /// �жϵ�����ѯ�����Ƿ���Ч
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        private static bool CheckConditionIsValid(QueryCondition condition)
        {
            return (condition.SubQuery != null && condition.SubQuery.Length > 0) || condition.Value != null || condition.CompareType == CompareType.Equal || condition.CompareType == CompareType.NotEqual || condition.CompareType == CompareType.Is || condition.CompareType == CompareType.IsNot;
        }

        /// <summary>
        /// ����Parameter����
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static SqlParameter CreateParameterObject(string name, object value)
        {
            return new SqlParameter(name, (value == null) ? DBNull.Value : value);
        }
    }
}
