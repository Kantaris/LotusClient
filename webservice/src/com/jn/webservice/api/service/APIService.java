package com.jn.webservice.api.service;

import javax.servlet.http.HttpServletRequest;
import javax.servlet.http.HttpServletResponse;

import org.apache.commons.codec.digest.DigestUtils;
import org.apache.commons.lang.StringUtils;
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Component;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.servlet.ModelAndView;
import org.w3c.dom.Document;
import org.w3c.dom.Element;

import com.jn.webservice.api.Constants;
import com.jn.webservice.api.Utils;

@Controller
@RequestMapping("/")
@Component("APIService")
public class APIService extends com.jn.webservice.api.Controller {
	
	@ExceptionHandler(value = Exception.class)
	public ModelAndView handlerException(HttpServletRequest req) {
		return new ModelAndView("error");
	}
	
	@ExceptionHandler(com.jn.webservice.api.bean.ExceptionHandler.class)
	public ModelAndView handleCustomException(ExceptionHandler ex) {
		return new ModelAndView("error");
	}

	@ExceptionHandler(com.jn.webservice.api.bean.ExceptionHandler.class)
	@ResponseStatus(value = HttpStatus.NOT_FOUND)
	public ModelAndView handle404Exception(ExceptionHandler ex) {
		return new ModelAndView("error");
	}
	
	@RequestMapping(value="password", produces = "text/xml;charset=UTF-8")
	public void password(HttpServletRequest req, HttpServletResponse res,
			@RequestParam(value = "salt", required = false) String salt,
			@RequestParam(value = "password", required = false) String password
			) {
		
		String str = "";
		
		salt = Utils.encode(Utils.checkNull(salt));
		password = Utils.encode(Utils.checkNull(password));

		try{
			String hash = DigestUtils.md5Hex(password + salt);
			
//			System.out.println(" Hash :" + StringUtils.rightPad(password, 30) + " , " + StringUtils.rightPad(salt, 30) + " = " + hash );
			
			Document doc = getDocument();
			if (doc == null){
				str = getResponseElement(Constants.ResponseStatus.BAD_CERTIFICATION);
			}else{
				Element root = getRootElement(doc);
				
				Element result = getResultElement(doc, Constants.APIType.TEST_PASSWORD);
				
				Element p = doc.createElement("password");
				p.appendChild(doc.createTextNode(password));
				result.appendChild(p);
				
				Element s = doc.createElement("salt");
				s.appendChild(doc.createTextNode(salt));
				result.appendChild(s);
				
				Element h = doc.createElement("hash");
				h.appendChild(doc.createTextNode(hash));
				result.appendChild(h);
				
				root.appendChild(result);
				
				Element status = getResponseElement(doc, Constants.ResponseStatus.SUCCESS);
				root.appendChild(status);
				
				doc.appendChild(root);
				str = makeXMLString(doc);
			}
			
			print(res, str);
		}catch (Exception e){ }
	}

	@RequestMapping(value="uuid", produces = "text/xml;charset=UTF-8")
	public void uuid(HttpServletRequest req, HttpServletResponse res
			) {
		String str = "";
		
		try{
			String uuid = Utils.getUUID(64);
			
			Document doc = getDocument();
			if (doc == null){
				str = getResponseElement(Constants.ResponseStatus.BAD_CERTIFICATION);
			}else{
				Element root = getRootElement(doc);
				
				Element result = getResultElement(doc, Constants.APIType.TEST_UUID);
				
				Element p = getSessionIDElement(doc, uuid);
				result.appendChild(p);
				
				root.appendChild(result);
				
				Element status = getResponseElement(doc, Constants.ResponseStatus.SUCCESS);
				root.appendChild(status);
				
				doc.appendChild(root);
				str = makeXMLString(doc);
			}
			
			print(res, str);
		}catch (Exception e){ }
	}
	
}
