package com.jn.webservice.api.dao;

import java.util.List;

import org.hibernate.Criteria;
import org.hibernate.Session;
import org.hibernate.criterion.Restrictions;
import org.springframework.stereotype.Repository;

import com.jn.webservice.api.DAO;
import com.jn.webservice.api.domain.ServerInfo;

@Repository("ServerDAO")
public class ServerDAO extends DAO {	
	
	public String insert(ServerInfo data){
		Session session = getCurrentSession();
		try{
			session.save(data);
		}catch (Exception e){
			return e.toString();
		}
		
		return "";
	}
	
	public String update(ServerInfo data){
		Session session = getCurrentSession();
		try{
			session.update(data);
		}catch (Exception e){
			return e.toString();
		}
		
		return "";
	}
	
	public List getServerList(){
		Criteria criteria = getCurrentSession().createCriteria(ServerInfo.class);
		return criteria.list();
	}
	
	public ServerInfo getServerInfo(Integer id){
		Criteria criteria = getCurrentSession().createCriteria(ServerInfo.class);
		criteria.add(Restrictions.eq("id", id)) ;
		return (ServerInfo)criteria.uniqueResult();
	}
}
