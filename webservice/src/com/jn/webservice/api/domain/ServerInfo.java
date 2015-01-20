package com.jn.webservice.api.domain;

import javax.persistence.Column;
import javax.persistence.Entity;
import javax.persistence.GeneratedValue;
import javax.persistence.GenerationType;
import javax.persistence.Id;
import javax.persistence.Table;

import org.w3c.dom.Document;
import org.w3c.dom.Element;

@Entity
@Table(name="servers")
public class ServerInfo {
	
	@Id
	@GeneratedValue(strategy=GenerationType.AUTO)
	@Column(name="id")
	private Integer id;

	@Column(name="title")
	private String title;

	@Column(name="passwordhash")
	private String passwordhash;

	@Column(name="passwordsalt")
	private String passwordsalt;

	@Column(name="name")
	private String name;
	
	@Column(name="address")
	private String address;

	@Column(name="port")
	private Integer port;

	@Column(name="country")
	private String country;

	@Column(name="continent")
	private String continent;
	
	@Column(name="hulu")
	private String hulu;
	
	@Column(name="image")
	private String image;

	public Integer getId() {
		return id;
	}

	public void setId(Integer id) {
		this.id = id;
	}

	public String getTitle() {
		return title;
	}

	public void setTitle(String title) {
		this.title = title;
	}

	public String getPasswordhash() {
		return passwordhash;
	}

	public void setPasswordhash(String passwordhash) {
		this.passwordhash = passwordhash;
	}

	public String getPasswordsalt() {
		return passwordsalt;
	}

	public void setPasswordsalt(String passwordsalt) {
		this.passwordsalt = passwordsalt;
	}

	public String getName() {
		return name;
	}

	public void setName(String name) {
		this.name = name;
	}

	public String getAddress() {
		return address;
	}

	public void setAddress(String address) {
		this.address = address;
	}

	public Integer getPort() {
		return port;
	}

	public void setPort(Integer port) {
		this.port = port;
	}

	public String getCountry() {
		return country;
	}

	public void setCountry(String country) {
		this.country = country;
	}

	public String getContinent() {
		return continent;
	}

	public void setContinent(String continent) {
		this.continent = continent;
	}

	public String getHulu() {
		return hulu;
	}

	public void setHulu(String hulu) {
		this.hulu = hulu;
	}

	public String getImage() {
		return image;
	}

	public void setImage(String image) {
		this.image = image;
	}
	
	public Element getElement(Document doc){
		Element result = doc.createElement("server");
		
		Element e_id = doc.createElement("id");
		e_id.appendChild(doc.createTextNode(String.valueOf(id)));
		result.appendChild(e_id);
		
		if (title.length()>0){
			Element e_title = doc.createElement("title");
			e_title.appendChild(doc.createTextNode(title));
			result.appendChild(e_title);
		}
		
		if (name.length()>0){
			Element e_name = doc.createElement("name");
			e_name.appendChild(doc.createTextNode(name));
			result.appendChild(e_name);
		}
		
		if (address.length()>0){
			Element e_address = doc.createElement("address");
			e_address.appendChild(doc.createTextNode(address));
			result.appendChild(e_address);
		}
		
		Element e_port = doc.createElement("port");
		e_port.appendChild(doc.createTextNode(String.valueOf(port)));
		result.appendChild(e_port);

		if (country.length()>0){
			Element e_country = doc.createElement("country");
			e_country.appendChild(doc.createTextNode(country));
			result.appendChild(e_country);
		}

		if (continent.length()>0){
			Element e_continent = doc.createElement("continent");
			e_continent.appendChild(doc.createTextNode(continent));
			result.appendChild(e_continent);
		}

		Element e_hulu = doc.createElement("hulu");
		if (hulu.equals("0"))
			e_hulu.appendChild(doc.createTextNode("No"));
		else
			e_hulu.appendChild(doc.createTextNode("Yes"));
		result.appendChild(e_hulu);

		if (image.length()>0){
			Element e_image = doc.createElement("image");
			e_image.appendChild(doc.createTextNode(image));
			result.appendChild(e_image);
		}

		return result;
	}
}
