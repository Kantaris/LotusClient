package com.jn.webservice.api;

import org.hibernate.Session;
import org.hibernate.SessionFactory;
import org.springframework.beans.factory.annotation.Autowired;

public class DAO {
	
	@Autowired
	private SessionFactory sessionFactory;

	public Session getCurrentSession() {
		return sessionFactory.getCurrentSession();
	}

	public void setSessionFactory(SessionFactory sessionFactory) {
		this.sessionFactory = sessionFactory;
	}

	public void clear() {
		try {
			sessionFactory.getCurrentSession().clear();
		} catch (Exception ee) {
		}
	}

	public void flush() {
		try {
			sessionFactory.getCurrentSession().flush();
		} catch (Exception ee) {
		}
	}
}
