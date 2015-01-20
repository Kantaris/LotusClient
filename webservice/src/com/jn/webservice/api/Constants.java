package com.jn.webservice.api;

public class Constants {
	public static final String Version = "1.0"; 
	public static final int EXPIRED_SESSION_TIME = 5;
	
	public static final int SERVER_PORT = 8443;
	
	public enum ResponseStatus {
		SUCCESS(0, "Success"),
		FAILED(1, "Failed"),
		
		BAD_CERTIFICATION(-1, "Bad Certification"),
		EXPIRED_CERTIFICATION(-2, "Expired Certification"),
		
		BAD_REQUEST(-3, "Bad Request"),
		
		INVALID_SESSION(120, "Invalid Session ID"),
		EXPIRED_SESSION(121, "Expired Session ID"),
		
		NONEXIST_KEY(130, "Invalid Key"),
		NONEXIST_CERT(131, "Invalid Cert"),
		INVALID_SERVER(110, "Invalid Server ID"),
		
		INVALID_USER(100, "Invalid User"),
		EXPIRED_USER(102, "Expired User"),
		INVALID_PASSWORD(101, "Invalid Password");
		
		private int code;
		private String msg;
		
		private ResponseStatus(final int code, final String msg){
			this.code = code;
			this.msg = msg;
		}
		
		public String code(){
			return String.valueOf(code);
		}
		
		public String msg(){
			return msg;
		}		
	};
	
	public enum APIType	{
		USER_LOGIN(1),
		USER_LOGOUT(2),
		
		KEY_GETOPENWEBKEY(3),
		
		TEST_PASSWORD(1001),
		TEST_UUID(1002);
		
		private int type;
		
		private APIType(final int type){
			this.type = type;
		}
		
		public String type(){
			return String.valueOf(type);
		}
	};
	
}
