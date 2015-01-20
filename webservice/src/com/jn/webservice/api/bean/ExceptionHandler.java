package com.jn.webservice.api.bean;

/**
 * <h1>Exception Class</h1>
 * 
 */
public class ExceptionHandler extends RuntimeException {

	private static final long serialVersionUID = 1L;

	private String errCode;
	private String errMsg;

	public ExceptionHandler(String errCode, String errMsg) {
		this.errCode = errCode;
		this.errMsg = errMsg;
	}

}
