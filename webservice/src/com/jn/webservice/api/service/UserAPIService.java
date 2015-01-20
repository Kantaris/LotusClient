package com.jn.webservice.api.service;

import java.util.List;

import javax.servlet.http.HttpServletRequest;
import javax.servlet.http.HttpServletResponse;

import org.apache.commons.codec.digest.DigestUtils;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Component;
import org.springframework.stereotype.Controller;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.w3c.dom.Document;
import org.w3c.dom.Element;

import com.jn.webservice.api.Constants;
import com.jn.webservice.api.Utils;
import com.jn.webservice.api.dao.ServerDAO;
import com.jn.webservice.api.dao.SessionDAO;
import com.jn.webservice.api.dao.UserDAO;
import com.jn.webservice.api.domain.ServerInfo;
import com.jn.webservice.api.domain.SessionInfo;
import com.jn.webservice.api.domain.UserInfo;

@Transactional
@Controller
@RequestMapping("User/")
@Component("UserAPIService")
public class UserAPIService extends com.jn.webservice.api.Controller {
	
	@Autowired
	UserDAO userDao; 

	@Autowired
	SessionDAO sessionDao; 

	@Autowired
	ServerDAO serverDao; 

	
	@RequestMapping(value="Login", produces = "text/xml;charset=UTF-8")
	public void index(HttpServletRequest req, HttpServletResponse res,
			@RequestParam(value = "username", required = false) String username,
			@RequestParam(value = "password", required = false) String password
			) {
		
		username = Utils.encode(Utils.checkNull(username));
		password = Utils.checkNull(password);
		
		String str = "";
		try{
			Document doc = getDocument();
			if (doc == null){
				str = getResponseElement(Constants.ResponseStatus.BAD_CERTIFICATION);
				
			}else{
				Element root = getRootElement(doc);

				Element status ;
				try{
					if (username.length()==0){
						status = getResponseElement(doc, Constants.ResponseStatus.INVALID_USER);
						
					}else if (password.length()==0){
						status = getResponseElement(doc, Constants.ResponseStatus.INVALID_PASSWORD);
						
					}else{
						
						if (!userDao.isExistUser(username)){
							status = getResponseElement(doc, Constants.ResponseStatus.INVALID_USER);
							
						} else {
							
							if (!userDao.isValidUser(username, Utils.getToday())){
								status = getResponseElement(doc, Constants.ResponseStatus.EXPIRED_USER);
								
							}else{
								UserInfo user = userDao.getUserInfo(username, Utils.getToday());
								
								if (!DigestUtils.md5Hex(password + user.getPasswordsalt()).equals(user.getPasswordhash())){
									status = getResponseElement(doc, Constants.ResponseStatus.INVALID_PASSWORD);
									
								} else {
									
									if (sessionDao.getUserCount(user.getId())>2){
										try{
											sessionDao.deleteSessionInfo(user.getId());
										}catch (Exception eesdf){}
									}
	
									String uuid = Utils.getKey(8);
									
									try{
										SessionInfo session = new SessionInfo();
										session.setSessionID(uuid);
										session.setUserID(user.getId());
										session.setStartTime(Utils.getTodayWithTime());
										
										String err = sessionDao.insert(session);
										if (err.length()>0)
											throw new Exception("");
	
										List list = serverDao.getServerList();
										Element servers = getServerElement(doc, list.size());
										
										for (int i=0; i<list.size(); i++){
											ServerInfo server =  (ServerInfo)list.get(i);
											servers.appendChild( server.getElement(doc) );
										}
	
//										Element result = getResultElement(doc, Constants.APIType.USER_LOGIN);
										
										Element p = getSessionIDElement(doc, uuid);
										root.appendChild(p);
										root.appendChild(servers);
	
										status = getResponseElement(doc, Constants.ResponseStatus.SUCCESS);
									}catch (Exception fsdfs){
										status = getResponseElement(doc, Constants.ResponseStatus.FAILED);
									}
								}
							}
						}
					}
				}catch (Exception ff){
					status = getResponseElement(doc, Constants.ResponseStatus.BAD_REQUEST);
				}

				root.appendChild(status);
				
				doc.appendChild(root);
				str = makeXMLString(doc);
			}
			
			print(res, str);
		}catch (Exception e){ }
	}
	
	
	@RequestMapping(value="Logout", produces = "text/xml;charset=UTF-8")
	public void logout(HttpServletResponse res,
			@RequestParam(value = "session_id", required = false) String sessionID) {

		sessionID = Utils.checkNull(sessionID);
		
		String str = "";
		try{
			Document doc = getDocument();
			if (doc == null){
				str = getResponseElement(Constants.ResponseStatus.BAD_CERTIFICATION);
				
			}else{
				Element root = getRootElement(doc);

				Element status ;
				
				try{
					
					if (sessionID.length()==0){
						status = getResponseElement(doc, Constants.ResponseStatus.INVALID_SESSION);
						
					}else{
						try{
							String err = sessionDao.deleteSessionInfo(sessionID);
							if (err.length()>0)
								throw new Exception("");
							
							status = getResponseElement(doc, Constants.ResponseStatus.SUCCESS);
						}catch (Exception fsdfs){
							status = getResponseElement(doc, Constants.ResponseStatus.FAILED);
						}
					}
					
				}catch (Exception f){
					status = getResponseElement(doc, Constants.ResponseStatus.BAD_REQUEST);
				}

				root.appendChild(status);
				
				doc.appendChild(root);
				str = makeXMLString(doc);
			}
			
			print(res, str);
		}catch (Exception e){ }
	}
	
}
