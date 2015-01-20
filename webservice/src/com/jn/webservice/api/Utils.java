package com.jn.webservice.api;

import java.text.SimpleDateFormat;
import java.util.*;

import javax.servlet.http.HttpServletRequest;

/**
 * <h1>Converter Class.</h1> 
 * 
 * @created 25-02-2010
 * @modified 12-03-2010
 * 		<p>several format of date, time, and etc.</p>
 * @modified 8/16/2014
 *		<p>add some functions.  getFieldValue, afterMonth, todayAfterMonth, etc.</p>		 		
 */


public class Utils {
	private static final String DEFAULT_STRING="";
	private static final int DEFAULT_INT=0;

	/**
	 * @param string
	 * @param def
	 * @return  if string is null then return space, else no changes.
	 */
    public static String checkNull(String string, String def) {
        if (string == null || string.trim().toLowerCase().equals("null") || string.trim().length()==0)
            return def;
        return string;
    }

	/**
	 * @param string
	 * @param def
	 * @return  if string is null then return space, else no changes.
	 */
    public static int checkNull(String string, int def) {
        if (string == null || string.equals(""))
            return def;
        return getInt(string);
    }
    
	/**
	 * @param string
	 * @param def
	 * @return  if string is null then return space, else no changes.
	 */
    public static long checkNull(String string, long def) {
        if (string == null || string.equals(""))
            return def;
        return getLong(string);
    }

    public static double checkNull(String string, double def) {
        if (string == null || string.equals(""))
            return def;
        return getDouble(string);
    }

    public static long checkNull(Long string, long def) {
        if (string == null)
            return def;
        return string.longValue();
    }

    public static double checkNull(Double string, double def) {
        if (string == null)
            return def;
        return string.doubleValue();
    }

    /**
     * @param string
	 * @return  if string is null then return space, else no changes.
     */
    public static String checkNull(String string) {
        return checkNull(string, DEFAULT_STRING);
    }

    /**
     * @param string
	 * @return  if string is null then return space, else no changes.
     */
    public static String checkNull(Object string) {
    	if (string==null)
    		return "";
        return checkNull(string.toString(), DEFAULT_STRING);
    }

    /**
     * @param string
	 * @return  if string is null then return space, else no changes.
     */
    public static String checkStringNULL(String string){
    	string = checkNull(string);
    	if (string.trim().equals("NULL") || string.trim().equals("null"))
    		return "";   
		return string;
    }
    
    /**
     * @return Current year		 			(Ex: 2009)
     */
    public static String getYear() {
        SimpleDateFormat sdf = new SimpleDateFormat("yyyy");
    	return sdf.format(new java.util.Date());
    }

    /**
     * @return Today	 			(Ex: 20090109)
     */
    public static String getToday() {
        SimpleDateFormat sdf = new SimpleDateFormat("yyyyMMdd");
    	return sdf.format(new java.util.Date());
    }

    /**
     * @return Today	 			(Ex: 2009-01-09)
     */
    public static String getTodayWithH() {
        SimpleDateFormat sdf = new SimpleDateFormat("yyyy-MM-dd");
    	return sdf.format(new java.util.Date());
    }
    
    /**
     * @return Current Month		 			(ex: 03)
     */
    public static String getMonth() {
        SimpleDateFormat sdf = new SimpleDateFormat("MM");
    	return sdf.format(new java.util.Date());
    }
    
    /**
     * @return Current Day		 			(ex: 07)
     */
    public static String getDay() {
        SimpleDateFormat sdf = new SimpleDateFormat("dd");
    	return sdf.format(new java.util.Date());
    }
    
    /**
     * @return Today		 			(ex: 2009-01-09 12:11)
     */
    public static String getTodayOfLine() {
        SimpleDateFormat sdf = new SimpleDateFormat("yyyy-MM-dd HH:mm");
    	return sdf.format(new java.util.Date());
    }

    /**
     * @return Today		 			(ex: 2009/01/09)
     */
    public static String getTodayOfSlas() {
        SimpleDateFormat sdf = new SimpleDateFormat("yyyy/MM/dd");
    	return sdf.format(new java.util.Date());
    }

    /**
     * @return Today, Time	 			(ex: 2009-01-09 12:29:10)
     */
    public static String getTimeIntoDataType() {
        SimpleDateFormat sdf = new SimpleDateFormat("yyyy-MM-dd HH:mm:ss");
    	return sdf.format(new java.util.Date());
    }
    
    
	/**
	 * @return current time			(ex: 2010, HHmm)
	 */
	public static String getTimeWithoutSecond() {
		SimpleDateFormat sdf = new SimpleDateFormat("HHmm");
		return sdf.format(new java.util.Date());
	}

    /**
     * @return Today, Time			(ex: 200901022330)
     */
	public static String getTodayWithTime() {
		SimpleDateFormat sdf = new SimpleDateFormat("yyyyMMddHHmm");
		return sdf.format(new java.util.Date());
	}

	/**
	 * @return Today, Time			(ex: 20090102212010)
	 */
	public static String getTodayWithSecTime() {
		SimpleDateFormat sdf = new SimpleDateFormat("yyyyMMddHHmmss");
		return sdf.format(new java.util.Date());
	}

	/**
	 * @return current time			(ex: 212010)
	 */
	public static String getTime() {
		SimpleDateFormat sdf = new SimpleDateFormat("HHmmss");
		return sdf.format(new java.util.Date());
	}

    /**
	 * @return current time 		(ex: 21:20:10)
     */
	public static String getTimeWithDot() {
		SimpleDateFormat sdf = new SimpleDateFormat("HH:mm:ss");
		return sdf.format(new java.util.Date());
	}

	/**
	 * if smaller than 10, 0
	 * 
	 * @param value 
	 * @return String
	 */
	public static String fillZero10(int value){
		String tem="";
		if (value>=10){
			tem=String.valueOf(value);
		}
		else{
			tem="0"+String.valueOf(value);
		}
		return tem;
	}
	public static String convertDateSlash(String year,String month,String day,String chr)
	{
		return year+chr+fillZero10(Integer.parseInt(month))+chr+fillZero10(Integer.parseInt(day));		
	}
	
	/**
	 * @param str
	 * @param len
	 * @return fill zero string,  equals specified length.
	 */
    public static String fillZero(String str, int len) {
        String tmp = "0000000000000000000000000"+str;
        return tmp.substring(tmp.length()-len);
    }

	/**
	 * @param str
	 * @param len
	 * @return fill zero string,  equals specified length.
	 */
    public static String fillZero(String str, String strLength) {
        int length = Integer.valueOf(strLength).intValue();    
        return fillZero(str, length);
    }

    /**
	 * fill zero string,  equals specified length.

	 * @param str 
	 * @param len 
	 * @return fill zero string,  equals specified length.
	 */
	public static String fillZero2End(String str, int len) {
		String tmp = str+"0000000000000000000000000";
		return tmp.substring(0,len);
	}

	public static String fillNine2End(String str, int len) {
		String tmp = str+"9999999999999999999999999";
		return tmp.substring(0,len);
	}

	/**
	 * convert string to integer.
	 * 
	 * @param value - String
	 * @return integer
	 */
	public static int getInt(String value){
		return Integer.parseInt(value);
	}
	
	public static int getInt(String value, int defaultVal){
		int val = defaultVal;
		try{
			val = Integer.parseInt(value);
		}catch(Exception e){
		}
		return val;
	}

	/**
	 * convert string to long.
	 * 
	 * @param value - String
	 * @return long
	 */
	public static long getLong(String value){
		return Long.parseLong(value);
	}

	public static double getDouble (String value){
		return Double.parseDouble(value);
	}

	public static double getDouble (String value, int digit){
		double a = Double.parseDouble(value);
		return Math.round(a*digit*100) / (100*digit);
	}

	/**
	 * convert double to integer.
	 * @param value - double
	 * @return integer
	 */
	public static int getInt(double value){
		return (int)value;
	}
	
	/**
	 * get bytes of utf-8
	 * 
	 * @param value - String
	 * @return bytes
	 */
	public static String encode(String value){
		String n_str=null;
		try{
			n_str=new String(value.getBytes("ISO-8859-1"),"UTF-8");
		}catch (Exception q){
			n_str="";
		}
		return n_str;
	}
   
	/**
	 * get end day of the month.
	 * 
	 * @return
	 */
	public static int getEndDayOfMonth(){
		int year = getInt(getYear());
		int month = getInt(getMonth());
		int days = 31;
	
		if (month == 1 || month ==3 || month == 5 || month == 7 || month == 8 || month == 10 || month == 12)
			days=31;
		else if (month==2){
			if (year % 400 == 0 )
				days = 29;
			else if (year % 100 == 0)
				days = 28;
			else if (year % 4 == 0 )
				days = 29;
			else
				days = 28;
		}else
			days=30;
		
		return days;
	}
	
	/**
	 * get end day of the month.
	 * 
	 * @param year 
	 * @param month
	 * @return INT
	 */
	public static int getEndDayOfMonth(int year, int month){
		int days = 31;
		
		if (month == 1 || month ==3 || month == 5 || month == 7 || month == 8 || month == 10 || month == 12)
			days=31;
		else if (month==2){
			if (year % 400 == 0 )
				days = 29;
			else if (year % 100 == 0)
				days = 28;
			else if (year % 4 == 0 )
				days = 29;
			else
				days = 28;
		}else
			days=30;
		
		return days;
	}

	/**
	 * 
	 * 
	 * @return INT()
	 */
	public static int getT(){
		int t = 0;
		int year = getInt(getYear());
		int month = getInt(getMonth());
		int days = 0;
		
		for (int i=2010; i<year; i++)
			days += getEndDayOfMonth(i, 2) + 337;
		for (int i=1; i<month; i++)
			days += getEndDayOfMonth(year, i);
		
		return (days + 5) % 7;
	}
	
	/**
	 * 
	 * 
	 * @param value 
	 * @return String
	 */
	public static String isValidated(String value){
		if (value.trim().equals("0"))
			return "";
		return value;
	}
	
    public static String checkVQL(String string){
    	char[] checkStr = { '<', '>', '(', ')', '{', '}', '[', ']', '~', '`', '!', '@', '#', '$', '%', '^', '&', '*', '|', '\\', '/', ',', ':', ';', '?', '='};
    	String str = "";
    	char ch = '0';
    	for( int i = 0; i < string.length(); i++ ){
			ch = string.charAt(i);
    		for( int j = 0; j < checkStr.length; j++ ){
    			if( ch == checkStr[j] ){
    				ch = ' ';
    				break;
    			}
    		}
    		str += ch;
    	}
    	return str;
    }
    
    public static String addVQuery(String query, String str, String op){
    	String tmp = "";
    	if( query.equals("") == true )
    		tmp = str;
    	else
    		tmp = query + " " + op + " " + str;
    	return tmp;
    }
    
    public static String toHTMLString(String s){
    	s = s.replaceAll("\n", "<br>");
    	s = s.replaceAll("\"", "&quot;");
    	
    	return s;
    }

    public static String getDayOfWeek(String date)
	{
		String week="";
		java.util.Calendar cl=Calendar.getInstance();
		
		cl.set(Integer.parseInt(date.substring(0,4)),
				Integer.parseInt(date.substring(5,7))-1,
				Integer.parseInt(date.substring(8,10)));		
		/*week=cl.getDisplayName(Calendar.DAY_OF_WEEK, java.util.Calendar.ALL_STYLES, Locale.KOREAN);*/
		
		if (cl.get(Calendar.DAY_OF_WEEK)==7)
			week="sun";
		else if (cl.get(Calendar.DAY_OF_WEEK)==1)
			week="mon";
		else if (cl.get(Calendar.DAY_OF_WEEK)==2)
			week="tue";
		else if (cl.get(Calendar.DAY_OF_WEEK)==3)
			week="wed";
		else if (cl.get(Calendar.DAY_OF_WEEK)==4)
			week="thu";
		else if (cl.get(Calendar.DAY_OF_WEEK)==5)
			week="fri";
		else if (cl.get(Calendar.DAY_OF_WEEK)==6)
			week="sat";
					
		return week;
	}
    
    public static String[] splitIds(String id) {
		String tmp[];
		String tmp1[];
		ArrayList<String> result = new ArrayList<String>();
		int i = 0, j = 0;
		int m, n;
		String tmp2;
		tmp = id.split(",");
		for (i = 0; i < tmp.length; i++) {
			if (tmp[i].indexOf("-") >= 0) {
				tmp1 = tmp[i].split("-");
				if (tmp1.length == 2) {
					if (tmp1[0].indexOf(".") > 0 && tmp1[1].indexOf(".") > 0) {
						tmp2 = tmp1[0].substring(0, tmp1[0].lastIndexOf("."));
						m = Integer.parseInt(tmp1[0].substring(tmp1[0].lastIndexOf(".") + 1));
						n = Integer.parseInt(tmp1[1].substring(tmp1[1].lastIndexOf(".") + 1));
						for (j = m; j < n; j++)
							result.add(tmp2 + "." + String.valueOf(j));
					} else if (tmp1[0].indexOf(".") == -1 && tmp1[1].indexOf(".") == -1) {
						m = Integer.parseInt(tmp1[0]);
						n = Integer.parseInt(tmp1[1]);
						for (j = m; j < n; j++)
							result.add(String.valueOf(j));
					}
				}
			} else
				result.add(tmp[i]);
		}
		if (result.size() > 0) {
			String resultStr[] = new String[result.size()];
			for (i = 0; i < result.size(); i++)
				resultStr[i] = result.get(i);
			return resultStr;
		} else
			return null;
	}

	public static int checkNull(Object object, int def) {
		// TODO Auto-generated method stub
		return checkNull(object.toString(), def);
	}
	
	public static String substring(int len, String text){
		if (text.length()<len)
			return text;
		return text.substring(0, len) + "...";
	}
	
	public static String getRelativeAgo(String today, String past){
		int t = getInt(today.substring(0,8));
		int p = getInt(past.substring(0,8));
		if (t == p)
			return getInt(past.substring(8,10)) + ":" + getInt(past.substring(10,12));
		if (t-p == 1)
			return "Yesterday";
		if (t-p == 7)
			return "A week ago";
		if (t-p == 30 || t-p == 31)
			return "A month ago";
		
		return (t-p) + " days ago";
	}
		
	public static String getDate(String date){
		int y = getInt(date.substring(0,4));
		int m = getInt(date.substring(4,6));
		int d = getInt(date.substring(6,8));
		
		int h = getInt(date.substring(8,10));
		return y + "/" + m + "/" + d + " " + h + ":" + date.substring(10,12);
	}
	
	public static String todayAfterMonth(int month){
		String str = getToday();
		int y = getInt(str.substring(0,4));
		int m = getInt(str.substring(4,6));
		int d = getInt(str.substring(6,8));
		int t;
		
		String s ="" ;
		y += (m+month)/12;
		if ( (m+month) %12 == 0){
			y -- ;
			s = fillZero( String.valueOf(12), 2) ;
			t = 12;
		}else{
			s = fillZero( String.valueOf( (m+month) %12 ), 2) ;
			t = (m+month) %12;
		}
		
		int k = getEndDayOfMonth(y, t);
		if (d>k)
			d = k;
		
		return y + s + "" + d;
	}
	
	/**
	 * 
	 * @param date - ex: 20140203
	 * @param month
	 * @param day
	 * @return
	 */
	public static String afterMonth(String date, int month, int day){
		String str = date;
		int y = getInt(str.substring(0,4));
		int m = getInt(str.substring(4,6));
		int d = getInt(str.substring(6,8));
		int t;
		
		String s ="" ;
		y += (m+month)/12;
		if ( (m+month) %12 == 0){
			y -- ;
			s = fillZero( String.valueOf(12), 2) ;
			t = 12;
		}else{
			s = fillZero( String.valueOf( (m+month) %12 ), 2) ;
			t = (m+month) %12;
		}
		
		int k = getEndDayOfMonth(y, t);
		
		if (day==0)
			day = d;
		d = day;
		if (day>k)
			d = k;
		
		return y + s + "" + d;
	}

	public static String beforeDay(String date, int day){
		try {
			SimpleDateFormat sdf = new SimpleDateFormat("yyyyMMdd");
			java.util.Date d = sdf.parse(date);
			d.setTime( d.getTime() - (long)day * 24 * 60 * 60 * 1000);
			date = sdf.format(d);
		} catch (Exception e) {
		}

		return date;
	}
	
	public static String getExtension(String file){
		int i = file.lastIndexOf(".");
		try{
			if (i!=-1){
				String ext = file.substring(i+1, file.length());
				if (ext.length()==3 || ext.length() == 4)
					return ext;
			}
		}catch (Exception e){}
		return "";
	}
	
	public static boolean isInList(String list, String value){
		try{
			if (list.trim().length()>0){
				String[] r = list.split(";;;");
				for (int i=0; i<r.length; i++){
					if (value.equals(r[i]))
						return true;
				}
			}
		}catch (Exception e){}

		return false;
	}

	public static String getClientIpAddr(HttpServletRequest request) {  
        String ip = request.getHeader("X-Forwarded-For");  
        if (ip == null || ip.length() == 0 || "unknown".equalsIgnoreCase(ip)) {  
            ip = request.getHeader("Proxy-Client-IP");  
        }  
        if (ip == null || ip.length() == 0 || "unknown".equalsIgnoreCase(ip)) {  
            ip = request.getHeader("WL-Proxy-Client-IP");  
        }  
        if (ip == null || ip.length() == 0 || "unknown".equalsIgnoreCase(ip)) {  
            ip = request.getHeader("HTTP_CLIENT_IP");  
        }  
        if (ip == null || ip.length() == 0 || "unknown".equalsIgnoreCase(ip)) {  
            ip = request.getHeader("HTTP_X_FORWARDED_FOR");  
        }  
        if (ip == null || ip.length() == 0 || "unknown".equalsIgnoreCase(ip)) {  
            ip = request.getRemoteAddr();  
        }  
        return ip;  
    }  

	public static String getScreenName(String str){
		String result = "";
		try{
			String[] a = str.split("/");
			result = a[a.length-1].replaceAll(".jsp", "");
		}catch (Exception e){}
		return result;
	}
	
	public static String  getNextID(long start, long end){
		try{
			for (long i=start; i<=end; i++){
				String n = String.valueOf(i);
				if (n.indexOf("4")<0)
					return n;
			}
		}catch (Exception e){}
		
		return "";
	}
	
	public static String toDisplayDate(String date){
		String result = "";
		
		try{
			if (date.indexOf("-")>-1)
				return date;
			
			if (date.length()>4){
				if (date.length()>6){
					if (date.length()>8){
						result = date.substring(0, 4) + "-" + date.substring(4,6) + "-" + date.substring(6, 8) + " " + date.substring(8);
					}else{
						result = date.substring(0, 4) + "-" + date.substring(4,6) + "-" + date.substring(6);
					}
				}else{
					result = date.substring(0, 4) + "-" + date.substring(4);
				}
			}else
				result = date;
		}catch (Exception e){}
		
		return result;
	}
	
	public static String fromDisplayDate(String date){
		return date.replaceAll("-", "");
	}
	
	public static int getDifferenceDays(String date1, String date2){
		date1 = fromDisplayDate(date1);
		date2 = fromDisplayDate(date2);
		
		try{
			return Utils.getInt(date1) - Utils.getInt(date2);
		}catch (Exception e){}
		
		return 0;
	}

	public static String toSearchDate(String date){
		return date + " 23:59";
	}
	
	public static String forGetMsg(String param, String start, String end){
		if (param.length()>0)
			param = start + param + end;
		
		return param;
	}
	
	public static Long getTimeStamp(String value){
		try{
			SimpleDateFormat sdf = new SimpleDateFormat("yyyyMMdd");
			java.util.Date date = sdf.parse(value);
			java.sql.Timestamp timestamp = new java.sql.Timestamp(date.getTime());
			
//			Log.i(timestamp.getTime() + "");
//			return timestamp.toString();
			return timestamp.getTime();
		}catch (Exception e){}
		
		return (long)0;
	}
	
	public static String getKey(int length){
		String data = "";
		String charset = "1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
		Random random = new Random();
		
		for (int i=0; i<length; i++){
			int index = random.nextInt(charset.length());
			data = data + charset.substring(index, index+1);
		}
		
		return data;
	}
	
	public static String getUUID(int length){
		int c = length / 32;
		String result = "";
		for (int i=0; i<c; i++)
			result += UUID.randomUUID().toString().replaceAll("-", "");
		
		if (result.length()>length)
			result = result.substring(0, length);
		return result;
	}
	
	public static int getDifferentTimes(String time1, String time2){
		String day1 = time1.substring(0, 8);
		String day2 = time2.substring(0, 8);
		
		if (!day1.equals(day2))
			return 1000;
		
		String hm1 = time1.substring(8);
		String hm2 = time2.substring(8);
		
		return getInt(hm1) - getInt(hm2);
	}
}
