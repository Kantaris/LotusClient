package com.jn.webservice.api.bean;

import javax.servlet.http.HttpSession;
import javax.servlet.http.HttpSessionEvent;
import javax.servlet.http.HttpSessionListener;

/**
 *  <h1>Session Listener Class</h1>
 *  <p>declared in <a>web.xml</a>. </p>
 *  
 * @created 6/12/2009
 * @modified 8/10/2014
 * 		<p>delete the unneccessary information</p>
 * 
 */

public class SessionListener implements HttpSessionListener {
	
	private int totalSessionCount = 0;
	private int currentSessionCount = 0;
	private int maxSessionCount = 0;
//	private HitCounter hitCounter = HitCounter.getInstance(); // 전체 방문자 수
	private final byte[] LOCK = new byte[0];
	
	public void sessionCreated(HttpSessionEvent event) {
		totalSessionCount++;
    	currentSessionCount++;
    	
		HttpSession session = event.getSession();

    	//hitCounter.increase();
		synchronized (LOCK) {
			if (currentSessionCount > maxSessionCount) {
				maxSessionCount = currentSessionCount;
			}
		}
		/*
		if (context == null) {
			context = session.getServletContext();
			context.setAttribute("sessionListener", this);
		}
		*/
		session.setMaxInactiveInterval(1500);	//1800
	}
	
	public void sessionDestroyed(HttpSessionEvent event) {
//	    HttpSession session = event.getSession();

	    try{
	    }catch (Exception q){}
	    
	    synchronized (LOCK) {
		    if (currentSessionCount > 0)
	            currentSessionCount--;
		}
	}
	
	public int getTotalSessionCount() {
		return totalSessionCount;
	}
	
	public int getCurrentSessionCount() {
		return currentSessionCount;
	}
	
	public int getMaxSessionCount() {
		return maxSessionCount;
	}
}