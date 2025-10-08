using Jsonata.Net.Native.Json;
using Jsonata.Net.Native;
using System.Collections.Generic;
using System;

namespace Jsonata.Net.Native.New
{

    public static class Utils 
    {
        public static bool isNumeric(JToken v) 
        {
            return v.Type == JTokenType.Integer || v.Type == JTokenType.Float;
        }

        public static bool isArrayOfStrings(Object v) 
        {
            throw new NotImplementedException();
            /*
            boolean result = false;
            if (v instanceof Collection) {
                for (Object o : ((Collection)v))
                    if (!(o instanceof String))
                        return false;
                return true;
            }
            return false;
            */
        }

        public static bool isArrayOfNumbers(Object v) 
        {
            throw new NotImplementedException();
            /*
            boolean result = false;
            if (v instanceof Collection) {
                for (Object o : ((Collection)v))
                    if (!isNumeric(o))
                        return false;
                return true;
            }
            return false;
            */
        }

        public static bool isFunction(Object o) 
        {
            throw new NotImplementedException();
            /*
            return o instanceof JFunction || o instanceof JFunctionCallable;
            */
        }

        /*
        static Object NONE = new Object();

        public static List<Object> createSequence() { return createSequence(NONE); }

        public static List<Object> createSequence(Object el) 
        {
            JList<Object> sequence = new JList<>();
            sequence.sequence = true;
            if (el!=NONE) {
                if (el instanceof List && ((List)el).size()==1)
                    sequence.add(((List)el).get(0));
                else
                // This case does NOT exist in Javascript! Why?
                    sequence.add(el);
            }
            return sequence;
        }

        public static class JList<E> extends ArrayList<E> {
            public JList() { super(); }
            public JList(int capacity) { super(capacity); }
            public JList(Collection<? extends E> c) {
                super(c);
            }

            // Jsonata specific flags
            public boolean sequence;

            public boolean outerWrapper;

            public boolean tupleStream;

            public boolean keepSingleton;

            public boolean cons;
        }

        public static boolean isSequence(Object result) {
            return result instanceof JList && ((JList)result).sequence;
        }
        */

        /**
            * List representing an int range [a,b]
            * Both sides are included. Read-only + immutable.
            * 
            * Used for late materialization of ranges.
        public static class RangeList extends AbstractList<Number> {

            final long a, b;
            final int size;

            public RangeList(long left, long right) {
                assert(left<=right);
                assert(right-left < Integer.MAX_VALUE);
                a = left; b = right;
                size = (int) (b-a+1);
            }

            @Override
            public int size() {
                return size;
            }

            @Override
            public boolean addAll(Collection<? extends Number> c) {
                throw new UnsupportedOperationException("RangeList does not support 'addAll'");
            }

            @Override
            public Number get(int index) {
                if (index < size) {
                    try {
                        return Utils.convertNumber( a + index );
                    } catch (JException e) {
                        // TODO Auto-generated catch block
                        e.printStackTrace();
                    }
                }
                throw new IndexOutOfBoundsException(index);
            }        
        }
        */

        /*
        public static void checkUrl(String str) {
            boolean isHigh = false;
            for ( int i=0; i<str.length(); i++) {
            boolean wasHigh = isHigh;
            isHigh = Character.isHighSurrogate(str.charAt(i));
            if (wasHigh && isHigh)
                throw new JException("Malformed URL", i);
            }
            if (isHigh)
            throw new JException("Malformed URL", 0);
        }

        static Object convertValue(Object val) {
            return val != Jsonata.NULL_VALUE ? val : null;
        }

        static void convertNulls(Map<String, Object> res) {
            for (Entry<String, Object> e : res.entrySet()) {
                Object val = e.getValue();
                Object l = convertValue(val);
                if (l!=val)
                    e.setValue(l);
                recurse(val);
            }
        }

        static void convertNulls(List<Object> res) {
            for (int i=0; i<res.size(); i++) {
                Object val = res.get(i);
                Object l = convertValue(val);
                if (l!=val)
                    res.set(i, l);
                recurse(val);
            }
        }

        static void recurse(Object val) {
            if (val instanceof Map)
                convertNulls((Map)val);
            if (val instanceof List)
                convertNulls((List)val);
        }

        public static Object convertNulls(Object res) {
            recurse(res);
            return convertValue(res);
        }

        
            * adapted from org.json.JSONObject https://github.com/stleary/JSON-java
        public static void quote(String string, StringBuilder w) {
            char b;
            char c = 0;
            String hhhh;
            int i;
            int len = string.length();

            for (i = 0; i < len; i += 1) {
                b = c;
                c = string.charAt(i);
                switch (c) {
                case '\\':
                case '"':
                    w.append('\\');
                    w.append(c);
                    break;
                / *
                case '/':
                    if (b == '<') {
                        w.append('\\');
                    }
                    w.append(c);
                    break;
                * /
                case '\b':
                    w.append("\\b");
                    break;
                case '\t':
                    w.append("\\t");
                    break;
                case '\n':
                    w.append("\\n");
                    break;
                case '\f':
                    w.append("\\f");
                    break;
                case '\r':
                    w.append("\\r");
                    break;
                default:
                    if (c < ' ' || (c >= '\u0080' && c < '\u00a0')
                            || (c >= '\u2000' && c < '\u2100')) {
                        w.append("\\u");
                        hhhh = Integer.toHexString(c);
                        w.append("0000", 0, 4 - hhhh.length());
                        w.append(hhhh);
                    } else {
                        w.append(c);
                    }
                }
            }
            */

    }
}
