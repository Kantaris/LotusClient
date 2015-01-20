package com.jn.webservice.api.dao;

import java.util.List;

import org.hibernate.Criteria;
import org.hibernate.Session;
import org.hibernate.criterion.Projections;
import org.hibernate.criterion.Restrictions;
import org.springframework.stereotype.Repository;

import com.jn.webservice.api.DAO;
import com.jn.webservice.api.domain.UserInfo;

@Repository("UserDAO")
public class UserDAO extends DAO {	
	
	public String insert(UserInfo data){
		Session session = getCurrentSession();
		try{
			session.save(data);
		}catch (Exception e){
			return e.toString();
		}
		
		return "";
	}
	
	public String update(UserInfo data){
		Session session = getCurrentSession();
		try{
			session.update(data);
		}catch (Exception e){
			return e.toString();
		}
		
		return "";
	}
	
	public boolean isExistUser(String username){
		Criteria criteria = getCurrentSession().createCriteria(UserInfo.class);
		criteria.add(Restrictions.eq("userName", username)) ;

		criteria.setProjection(Projections.rowCount());
		try{
			Long result = (Long)criteria.uniqueResult();
			if (result==null)
				return false;
			
			return result==1 ? true : false;
		}catch (Exception e){}

		return false;
	}

	public boolean isValidUser(String username, String date){
		Criteria criteria = getCurrentSession().createCriteria(UserInfo.class);
		criteria.add(Restrictions.eq("userName", username)) ;
		criteria.add(Restrictions.ge("activeUtil", date)) ;
		
		criteria.setProjection(Projections.rowCount());
		try{
			Long result = (Long)criteria.uniqueResult();
			if (result==null)
				return false;
			
			return result==1 ? true : false;
		}catch (Exception e){}
		
		return false;
	}

	public UserInfo getUserInfo(String username, String date){
		Criteria criteria = getCurrentSession().createCriteria(UserInfo.class);
		criteria.add(Restrictions.eq("userName", username)) ;
		criteria.add(Restrictions.ge("activeUtil", date)) ;
		return (UserInfo)criteria.uniqueResult();
	}
}
