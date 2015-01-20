package com.jn.webservice.api.dao;

import java.util.List;

import org.hibernate.Criteria;
import org.hibernate.Query;
import org.hibernate.Session;
import org.hibernate.criterion.Projections;
import org.hibernate.criterion.Restrictions;
import org.springframework.stereotype.Repository;

import com.jn.webservice.api.DAO;
import com.jn.webservice.api.domain.SessionInfo;
import com.jn.webservice.api.domain.UserInfo;

@Repository("SessionDAO")
public class SessionDAO extends DAO {	
	
	public String insert(SessionInfo data){
		Session session = getCurrentSession();
		try{
			session.save(data);
		}catch (Exception e){
			return e.toString();
		}
		
		return "";
	}
	
	public String update(SessionInfo data){
		Session session = getCurrentSession();
		try{
			session.update(data);
		}catch (Exception e){
			return e.toString();
		}
		
		return "";
	}
	
	public long getUserCount(Integer userID){
		Criteria criteria = getCurrentSession().createCriteria(SessionInfo.class);
		criteria.add(Restrictions.eq("userID", userID)) ;

		criteria.setProjection(Projections.rowCount());
		try{
			Long result = (Long)criteria.uniqueResult();  return result==null ? (long)0 : result;
		}catch (Exception e){}
		
		return 0;
	}

	public String deleteSessionInfo(Integer userID){
		try{
			String sql =  " delete from sessions where user_id=" + userID + " " ;
		    Query query = getCurrentSession().createSQLQuery(sql);
		    query.executeUpdate();
		}catch (Exception e){
			return e.toString();
		}
		
		return "";
	}

	public String deleteSessionInfo(String sessionID){
		try{
			String sql =  " delete from sessions where session_id=" + sessionID + " " ;
		    Query query = getCurrentSession().createSQLQuery(sql);
		    query.executeUpdate();
		}catch (Exception e){
			return e.toString();
		}
		
		return "";
	}
	
	public SessionInfo getSessionInfo(String sessionID){
		Criteria criteria = getCurrentSession().createCriteria(SessionInfo.class);
		criteria.add(Restrictions.eq("sessionID", sessionID)) ;
		return (SessionInfo)criteria.uniqueResult();
	}
}
